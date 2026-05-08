using OpenAI;
using OpenAI.Chat;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Service for generating action recommendations from themes using GPT function calling
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
    /// Generate action recommendations for a theme
    /// </summary>
    public async Task<List<ActionRecommendationResult>> GenerateRecommendationsAsync(
        Theme theme,
        List<FeedbackItem> relatedItems,
        List<string> productFeatures = null)
    {
        if (theme == null)
            throw new ArgumentNullException(nameof(theme));

        var chatClient = _client.GetChatClient("gpt-4.1-mini");

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
Average urgency: {relatedItems.Average(f => f.UrgencyScore ?? 0):F2}
Average sentiment: {relatedItems.Average(f => f.SentimentScore ?? 0):F2}

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

        var response = await chatClient.CompleteChatAsync(
            new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(@"You are an expert product manager. Generate actionable recommendations based on customer feedback themes. Always return valid JSON arrays."),
                ChatMessage.CreateUserMessage(userPrompt)
            }
        );

        var resultText = response.Value.Content[0].Text;
        return ParseRecommendationsResponse(resultText);
    }

    /// <summary>
    /// Evaluate usefulness of a recommendation
    /// </summary>
    public async Task<double> EvaluateUsefulnessAsync(
        ActionRecommendation recommendation,
        List<FeedbackItem> relatedItems)
    {
        if (relatedItems.Count == 0)
            return 3.0;

        var chatClient = _client.GetChatClient("gpt-4.1-mini");

        var systemPrompt = @"Rate the usefulness and value of an action recommendation. Consider:
- How well it addresses the customer feedback
- How many customers would benefit
- Feasibility and complexity
- Alignment with product goals
- Potential ROI

Return ONLY a JSON object:
{
  ""usefulnessScore"": 1,
  ""reasoning"": ""explanation""
}";

        var userPrompt = $@"Recommendation: {recommendation.Title}
Description: {recommendation.Description}
Category: {recommendation.Category}
Priority: {recommendation.Priority}
Estimated Effort: {recommendation.EstimatedEffort}/5
Impact Score: {recommendation.ImpactScore}

Number of feedback items addressed: {relatedItems.Count}

Rate the usefulness of this recommendation (1-5):";

        var response = await chatClient.CompleteChatAsync(
            new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(userPrompt)
            }
        );

        var resultText = response.Value.Content[0].Text;

        try
        {
            var jsonStart = resultText.IndexOf('{');
            var jsonEnd = resultText.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd >= 0)
            {
                var jsonStr = resultText.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var jsonDoc = JsonDocument.Parse(jsonStr);
                var score = jsonDoc.RootElement.GetProperty("usefulnessScore").GetDouble();
                return Math.Clamp(score, 1, 5);
            }
        }
        catch { }

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
