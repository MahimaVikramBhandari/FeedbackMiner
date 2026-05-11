using OpenAI;
using OpenAI.Chat;
using System.Text.Json;

/// <summary>
/// Service for automatically labeling and describing themes from feedback clusters
/// </summary>
public class ThemeLabelingService
{
    private readonly OpenAIClient _client;

    public ThemeLabelingService(OpenAIService openAIService = null)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            throw new Exception("OpenAI API key not found in environment variables.");

        _client = new OpenAIClient(apiKey);
    }

    /// <summary>
    /// Generate theme label and description from feedback cluster
    /// </summary>
    public async Task<ThemeLabelingResult> LabelClusterAsync(
        List<FeedbackItem> clusterItems,
        int maxExamples = 5)
    {
        if (clusterItems == null || clusterItems.Count == 0)
        {
            return new ThemeLabelingResult
            {
                Label = "Unlabeled Theme",
                Description = "Unable to analyze feedback",
                RelevanceScore = 1.0,
                Keywords = new List<string>(),
                AffectedAreas = new List<string>(),
                AffectedSegments = new List<string>(),
                CommonPatterns = new List<string>()
            };
        }

        try
        {
                var chatClient = _client.GetChatClient("gpt-4.1-mini");

                // Select representative examples (avoiding null or empty text)
                var examples = clusterItems
                    .Where(f => !string.IsNullOrWhiteSpace(f.ProcessedText))
                    .OrderByDescending(f => f.SentimentScore.HasValue ? Math.Abs(f.SentimentScore.Value) : 0)
                    .Take(maxExamples)
                    .Select(f => f.ProcessedText)
                    .ToList();

                if (examples.Count == 0)
                {
                    return new ThemeLabelingResult
                    {
                        Label = "Empty Feedback",
                        Description = "Cluster contains no processable feedback",
                        RelevanceScore = 1.0
                    };
                }

                var examplesText = string.Join("\n\n", examples.Select((e, i) => $"{i + 1}. {e}"));

                var systemPrompt = @"Analyze the following feedback messages and create a concise theme label and description.
                                        Return a JSON object with exactly these fields:
                                        {
                                          ""label"": ""<short theme name, 2-4 words, e.g., 'Performance Issues', 'Poor Documentation'>"",
                                          ""description"": ""<detailed description of the theme, 1-2 sentences>"",
                                          ""relevanceScore"": <number between 1 and 5 where 5 is highest relevance>,
                                          ""keywords"": [""list"", ""of"", ""key"", ""words""],
                                          ""affectedAreas"": [""list"", ""of"", ""product"", ""areas""],
                                          ""affectedSegments"": [""list"", ""of"", ""customer"", ""segments""],
                                          ""commonPatterns"": [""pattern1"", ""pattern2""]
                                        }

                                        Focus on:
                                        - Creating a clear, actionable theme name
                                        - Identifying the core issue
                                        - Noting affected areas and customer segments
                                        - Finding common patterns across the feedback
                                        - Using short, stable, business-friendly terminology 
                                        - Avoiding creative wording or unnecessary synonyms 
                                        - Returning consistent labels for semantically similar feedback

                                        If multiple labels are possible, choose the simplest and most generic business category. Keep labels under 4 words.";
                        

                var userPrompt = $@"Analyze this cluster of {clusterItems.Count} similar feedback messages:{examplesText} Generate theme insights:";
                var options = new ChatCompletionOptions
                {
                    Temperature = 0.2f
                };

                var response = await chatClient.CompleteChatAsync(
                        new List<ChatMessage>
                        {
                            ChatMessage.CreateSystemMessage(systemPrompt),
                            ChatMessage.CreateUserMessage(userPrompt)
                        },options
                    );

                    var resultText = response?.Value?.Content?[0]?.Text;
                    return ParseThemeLabelingResponse(resultText);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error labeling cluster: {ex.Message}");
            // Return safe default on failure
            return new ThemeLabelingResult
            {
                Label = "Auto-Generated Theme",
                Description = $"Theme from {clusterItems.Count} feedback items",
                RelevanceScore = 3.0,
                Keywords = new List<string> { "feedback", "theme" },
                AffectedAreas = new List<string>(),
                AffectedSegments = new List<string>(),
                CommonPatterns = new List<string>()
            };
        }
    }

    private ThemeLabelingResult ParseThemeLabelingResponse(string responseText)
    {
        try
        {
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');

            if (jsonStart < 0 || jsonEnd < 0)
                return new ThemeLabelingResult { Label = "General Feedback", Description = "Unlabeled theme" };

            var jsonStr = responseText.Substring(jsonStart, jsonEnd - jsonStart + 1);
            var jsonDoc = JsonDocument.Parse(jsonStr);
            var root = jsonDoc.RootElement;

            var result = new ThemeLabelingResult
            {
                Label = root.TryGetProperty("label", out var label) ? label.GetString() : "Feedback Theme",
                Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                RelevanceScore = root.TryGetProperty("relevanceScore", out var rel) ? rel.GetDouble() : 3.0,
                Keywords = ExtractStringArray(root, "keywords"),
                AffectedAreas = ExtractStringArray(root, "affectedAreas"),
                AffectedSegments = ExtractStringArray(root, "affectedSegments"),
                CommonPatterns = ExtractStringArray(root, "commonPatterns")
            };

            return result;
        }
        catch (Exception ex)
        {
            return new ThemeLabelingResult
            {
                Label = "Analysis Error",
                Description = $"Failed to label theme: {ex.Message}"
            };
        }
    }

    private List<string> ExtractStringArray(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray()
                .Select(e => e.GetString())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }
        return new List<string>();
    }
}

/// <summary>
/// Result of theme labeling
/// </summary>
public class ThemeLabelingResult
{
    public string Label { get; set; }
    public string Description { get; set; }
    public double RelevanceScore { get; set; } = 3.0;
    public List<string> Keywords { get; set; } = new List<string>();
    public List<string> AffectedAreas { get; set; } = new List<string>();
    public List<string> AffectedSegments { get; set; } = new List<string>();
    public List<string> CommonPatterns { get; set; } = new List<string>();
}
