using OpenAI;
using OpenAI.Chat;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Service for generating action recommendations from themes using GPT function calling
/// Includes comprehensive error handling for API calls
/// </summary>
public class ActionRecommendationService
{
    private readonly OpenAIClient _client;

    public ActionRecommendationService()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            throw new Exception("OpenAI API key not found in environment variables.");

        _client = new OpenAIClient(apiKey);
    }

    /// <summary>
    /// Generate action recommendations for a theme with error handling
    /// </summary>
    public async Task<List<ActionRecommendationResult>> GenerateRecommendationsAsync(
        Theme theme,
        List<FeedbackItem> relatedItems,
        List<string> productFeatures = null)
    {
        if (theme == null)
            throw new ArgumentNullException(nameof(theme), "Theme cannot be null");

        if (relatedItems == null || relatedItems.Count == 0)
        {
            Console.WriteLine($"Warning: Theme {theme.Label} has no related feedback items");
            return new List<ActionRecommendationResult>();
        }

        try
        {
            var sampleFeedback = string.Join("\n", 
                relatedItems.Take(5).Select(f => $"- {f.ProcessedText}"));

            var featuresContext = productFeatures != null && productFeatures.Count > 0
                ? $"\n\nAvailable product features:\n{string.Join("\n", productFeatures.Take(10))}"
                : "";

            var jsonFormat = @"{
                                  ""title"": ""..."",
                                  ""description"": ""..."",
                                  ""category"": ""..."",
                                  ""priority"": ""..."",
                                  ""estimatedEffort"": 1,
                                  ""impactScore"": 1,
                                  ""affectedAreas"": ""..."",
                                  ""benefitSegments"": ""...""
                                }";

            var userPrompt = $@"Generate 3-5 concrete action recommendations to address this customer feedback theme:

                                Theme: {theme.Label}
                                Description: {theme.Description}
                                Feedback count: {relatedItems.Count}
                                Average urgency: {(relatedItems.Any() ? relatedItems.Average(f => f.UrgencyScore ?? 0) : 0):F2}
                                Average sentiment: {(relatedItems.Any() ? relatedItems.Average(f => f.SentimentScore ?? 0) : 0):F2}

                                Sample feedback:
                                {sampleFeedback}{featuresContext}

                                For each recommendation, provide:
                                1. A clear, actionable title
                                2. Detailed description of the action
                                3. Category (Bug Fix, Feature Request, Process Improvement, UI/UX Enhancement, Documentation)
                                4. Priority (Low, Medium, High, Critical)
                                5. Estimated effort level (1-5)
                                6. Potential impact score (1-5)
                                7. Affected product areas (comma-separated list)
                                8. Customer segments that would benefit (comma-separated list)

                                Format each recommendation as JSON:
                                {jsonFormat}

                                Return all recommendations as a JSON array.";

            var chatClient = _client.GetChatClient("gpt-4.1-mini");
            var options = new ChatCompletionOptions { Temperature = 0.2f };

            var response = await chatClient.CompleteChatAsync(
                new List<ChatMessage>
                {
                    ChatMessage.CreateSystemMessage(@"You are an expert product manager. Generate actionable recommendations based on customer feedback themes. Always return valid JSON arrays."),
                    ChatMessage.CreateUserMessage(userPrompt)
                }, options
            );

            var resultText = response?.Value?.Content?[0]?.Text;
            if (string.IsNullOrWhiteSpace(resultText))
                return new List<ActionRecommendationResult>();

            return ParseRecommendationsResponse(resultText);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error generating recommendations for theme '{theme.Label}': {ex.Message}");
            // Return empty list on failure - better than crashing the pipeline
            return new List<ActionRecommendationResult>();
        }
    }

    /// <summary>
    /// Evaluate usefulness of a recommendation with error handling
    /// Includes fallback logic and edge case handling for small feedback sets
    /// </summary>
    public async Task<double> EvaluateUsefulnessAsync(
        ActionRecommendation recommendation,
        List<FeedbackItem> relatedItems)
    {
        // Edge case: no related items means default medium usefulness
        if (relatedItems == null || relatedItems.Count == 0)
            return 3.0;

        if (recommendation == null)
            return 2.0;

        try
        {
            // For very small feedback sets (1-2 items), use a simpler heuristic
            // based on recommendation properties rather than calling GPT every time
            if (relatedItems.Count <= 2)
            {
                return CalculateUsefulnessHeuristic(recommendation, relatedItems.Count);
            }

            var chatClient = _client.GetChatClient("gpt-4.1-mini");
            var options = new ChatCompletionOptions { Temperature = 0.2f };

            // Build comprehensive feedback summary
            var feedbackSummary = relatedItems.Count > 5
                ? string.Join("\n", relatedItems.Take(5).Select((f, i) => $"{i + 1}. {f.ProcessedText}"))
                : string.Join("\n", relatedItems.Select((f, i) => $"{i + 1}. {f.ProcessedText}"));

            var systemPrompt = @"You are an expert product manager evaluating recommendations. Rate the usefulness of an action recommendation considering:
                                - How comprehensively it addresses the customer feedback
                                - Number of affected customers
                                - Feasibility and effort required
                                - Potential business impact
                                - Risk-reward ratio

                                Be objective and base scoring on evidence in the feedback.

                                Return ONLY a JSON object:
                                {
                                  ""usefulnessScore"": 3.5,
                                  ""reasoning"": ""brief explanation""
                                }";

                                            var userPrompt = $@"Recommendation: {recommendation.Title}
                                Category: {recommendation.Category}
                                Priority: {recommendation.Priority}
                                Estimated Effort: {recommendation.EstimatedEffort}/5
                                Impact Score: {recommendation.ImpactScore}/5

                                Number of customer feedback items: {relatedItems.Count}
                                Feedback samples:
                                {feedbackSummary}

                                Rate the usefulness (1-5) of implementing this recommendation based on the above feedback:";

            var response = await chatClient.CompleteChatAsync(
                new List<ChatMessage>
                {
                    ChatMessage.CreateSystemMessage(systemPrompt),
                    ChatMessage.CreateUserMessage(userPrompt)
                },options
            );

            var resultText = response?.Value?.Content?[0]?.Text;
            return ParseUsefulnessScore(resultText);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error evaluating usefulness for recommendation '{recommendation?.Title}': {ex.Message}");
            // Fallback to heuristic if API call fails
            return CalculateUsefulnessHeuristic(recommendation, relatedItems?.Count ?? 0);
        }
    }

    /// <summary>
    /// Calculate usefulness score using heuristics when GPT is unavailable or for small datasets
    /// </summary>
    private double CalculateUsefulnessHeuristic(ActionRecommendation recommendation, int feedbackCount)
    {
        if (recommendation == null)
            return 2.0;

        double score = 3.0; // Base score

        // Impact bonus
        if (recommendation.ImpactScore >= 4)
            score += 1.0;
        else if (recommendation.ImpactScore >= 3)
            score += 0.5;

        // Effort bonus (lower effort = higher usefulness)
        if (recommendation.EstimatedEffort <= 2)
            score += 0.5;
        else if (recommendation.EstimatedEffort <= 3)
            score += 0.25;

        // Scale by feedback count
        score += Math.Min(1.0, feedbackCount / 10.0);

        return Math.Clamp(score, 1.0, 5.0);
    }

    private double ParseUsefulnessScore(string responseText)
    {
        try
        {
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = responseText.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var jsonDoc = JsonDocument.Parse(jsonStr);

                if (jsonDoc.RootElement.TryGetProperty("usefulnessScore", out var scoreElement))
                {
                    var score = scoreElement.GetDouble();
                    return Math.Clamp(score, 1.0, 5.0);
                }
            }
        }
        catch { }

        // Default fallback
        return 3.0;
    }

    private List<ActionRecommendationResult> ParseRecommendationsResponse(string responseText)
    {
        try
        {
            // Find JSON array
            var arrayStart = responseText.IndexOf('[');
            var arrayEnd = responseText.LastIndexOf(']');

            if (arrayStart < 0 || arrayEnd < 0)
                return new List<ActionRecommendationResult>();

            var jsonStr = responseText.Substring(arrayStart, arrayEnd - arrayStart + 1);
            var jsonDoc = JsonDocument.Parse(jsonStr);

            var results = new List<ActionRecommendationResult>();

            if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in jsonDoc.RootElement.EnumerateArray())
                {
                    results.Add(ParseSingleRecommendation(element));
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            return new List<ActionRecommendationResult>();
        }
    }

    private ActionRecommendationResult ParseSingleRecommendation(JsonElement element)
    {
        return new ActionRecommendationResult
        {
            Title = element.TryGetProperty("title", out var title) ? title.GetString() : "Untitled",
            Description = element.TryGetProperty("description", out var desc) ? desc.GetString() : "",
            Category = element.TryGetProperty("category", out var cat) ? cat.GetString() : "Process Improvement",
            Priority = element.TryGetProperty("priority", out var pri) ? pri.GetString() : "Medium",
            EstimatedEffort = element.TryGetProperty("estimatedEffort", out var eff) ? (int)eff.GetDouble() : 3,
            ImpactScore = element.TryGetProperty("impactScore", out var imp) ? imp.GetDouble() : 3,
            AffectedAreas = element.TryGetProperty("affectedAreas", out var areas) ? areas.GetString() : "",
            BenefitSegments = element.TryGetProperty("benefitSegments", out var segs) ? segs.GetString() : ""
        };
    }
}

/// <summary>
/// Result of action recommendation generation
/// </summary>
public class ActionRecommendationResult
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string Priority { get; set; }
    public int EstimatedEffort { get; set; }
    public double ImpactScore { get; set; }
    public string AffectedAreas { get; set; }
    public string BenefitSegments { get; set; }
}
