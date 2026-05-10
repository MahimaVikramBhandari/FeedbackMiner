using OpenAI;
using OpenAI.Chat;
using System.ComponentModel;
using System.Text.Json;

/// <summary>
/// Service for sentiment analysis and urgency extraction using GPT
/// </summary>
public class SentimentAnalysisService
{
    private readonly OpenAIClient _client;

    public SentimentAnalysisService()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            throw new Exception("OpenAI API key not found in environment variables.");

        _client = new OpenAIClient(apiKey);
    }

    /// <summary>
    /// Analyze sentiment and urgency of feedback text
    /// </summary>
    public async Task<SentimentAnalysisResult> AnalyzeAsync(string feedbackText, string context = null)
    {
        if (string.IsNullOrWhiteSpace(feedbackText))
            throw new ArgumentException("Feedback text cannot be empty", nameof(feedbackText));

        var chatClient = _client.GetChatClient("gpt-4.1-mini");
        var options = new ChatCompletionOptions { Temperature = 0.2f };

        var systemPrompt = @"Analyze the sentiment and urgency level of the given feedback.

                            Return a JSON object with exactly these fields:
                            {
                              ""sentimentScore"": <number between -1 (very negative) and 1 (very positive)>,
                              ""sentimentLabel"": ""Positive"" | ""Negative"" | ""Neutral"",
                              ""urgencyScore"": <number between 0 (not urgent) and 1 (very urgent)>,
                              ""urgencyLevel"": ""Low"" | ""Medium"" | ""High"" | ""Critical"",
                              ""reasoning"": ""brief explanation"",
                              ""keywords"": [""list"", ""of"", ""key"", ""indicators""]
                            }

                            Consider:
                            - For sentiment: tone, complaint language, praise, satisfaction indicators
                            - For urgency: mentions of outages, system down, critical issues, deadlines, financial impact, customer churn risk
                            - If feedback contains multiple issues, identify all major concerns 
                            - Return consistent scoring for semantically similar feedback 
                            - Focus on business impact and customer experience severity 

                            Guidelines: 
                            - Use Neutral only when sentiment is genuinely balanced 
                            - Use Critical urgency only for severe operational or financial impact 
                            - Keep reasoning concise and objective";

        var userPrompt = $"Analyze this feedback:\n\n{feedbackText}";
        if (!string.IsNullOrEmpty(context))
            userPrompt += $"\n\nContext: {context}";

        var response = await chatClient.CompleteChatAsync(
            new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(userPrompt)
            },options
        );

        var resultText = response.Value.Content[0].Text;
        return ParseSentimentResponse(resultText);
    }

    /// <summary>
    /// Batch analyze sentiment for multiple feedback items
    /// </summary>
    public async Task<List<SentimentAnalysisResult>> AnalyzeBatchAsync(List<string> feedbackTexts)
    {
        if (feedbackTexts == null || feedbackTexts.Count == 0)
            throw new ArgumentException("Feedback list cannot be empty", nameof(feedbackTexts));

        var results = new List<SentimentAnalysisResult>();
        const int batchSize = 5;

        for (int i = 0; i < feedbackTexts.Count; i += batchSize)
        {
            var batch = feedbackTexts.Skip(i).Take(batchSize).ToList();
            var batchResults = await Task.WhenAll(
                batch.Select(text => AnalyzeAsync(text))
            );

            results.AddRange(batchResults);
        }

        return results;
    }

    private SentimentAnalysisResult ParseSentimentResponse(string responseText)
    {
        try
        {
            // Extract JSON from response
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');

            if (jsonStart < 0 || jsonEnd < 0)
                return new SentimentAnalysisResult
                {
                    SentimentScore = 0,
                    SentimentLabel = "Neutral",
                    UrgencyScore = 0.5,
                    UrgencyLevel = "Medium",
                    Reasoning = "Could not parse response"
                };

            var jsonStr = responseText.Substring(jsonStart, jsonEnd - jsonStart + 1);
            var jsonDoc = JsonDocument.Parse(jsonStr);
            var root = jsonDoc.RootElement;

            return new SentimentAnalysisResult
            {
                SentimentScore = root.GetProperty("sentimentScore").GetDouble(),
                SentimentLabel = root.GetProperty("sentimentLabel").GetString() ?? "Neutral",
                UrgencyScore = root.GetProperty("urgencyScore").GetDouble(),
                UrgencyLevel = root.GetProperty("urgencyLevel").GetString() ?? "Medium",
                Reasoning = root.TryGetProperty("reasoning", out var reasoning) ? reasoning.GetString() : "",
                Keywords = root.TryGetProperty("keywords", out var keywords) 
                    ? keywords.EnumerateArray().Select(k => k.GetString()).ToList() 
                    : new List<string>()
            };
        }
        catch (Exception ex)
        {
            return new SentimentAnalysisResult
            {
                SentimentScore = 0,
                SentimentLabel = "Neutral",
                UrgencyScore = 0.5,
                UrgencyLevel = "Medium",
                Reasoning = $"Error parsing response: {ex.Message}"
            };
        }
    }
}

/// <summary>
/// Result of sentiment analysis
/// </summary>
public class SentimentAnalysisResult
{
    public double SentimentScore { get; set; } // -1 to 1
    public string SentimentLabel { get; set; } // Positive, Negative, Neutral
    public double UrgencyScore { get; set; } // 0 to 1
    public string UrgencyLevel { get; set; } // Low, Medium, High, Critical
    public string Reasoning { get; set; }
    public List<string> Keywords { get; set; } = new List<string>();
}
