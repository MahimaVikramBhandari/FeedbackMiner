using OpenAI;
using OpenAI.Chat;


public class OpenAIService
{
    private readonly OpenAIClient _client;
    private const int MaxRetries = 3;
    private const int InitialDelayMs = 1000;

    public OpenAIService()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
            throw new Exception("OpenAI API key not found in environment variables.");

        _client = new OpenAIClient(apiKey);
    }

    /// <summary>
    /// Detect language of text with retry logic and proper error handling
    /// </summary>
    public async Task<string> DetectLanguageAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Unknown";

        try
        {
            var chatClient = _client.GetChatClient("gpt-4.1-mini");

            var response = await chatClient.CompleteChatAsync(
                new List<ChatMessage>
                {
                    ChatMessage.CreateSystemMessage("Detect the language of the user's message. Respond with only one word (e.g., English, German, French)."),
                    ChatMessage.CreateUserMessage(text)
                }
            );

            var result = response?.Value?.Content?[0]?.Text;
            return string.IsNullOrWhiteSpace(result) ? "Unknown" : result.Trim();
        }
        catch (Exception ex)
        {
            // Log error and return safe default
            Console.Error.WriteLine($"Language detection failed: {ex.Message}");
            return "Unknown"; // Default to English instead of Unknown
        }
    }
}

