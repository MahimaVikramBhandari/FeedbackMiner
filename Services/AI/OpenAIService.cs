using OpenAI;
using OpenAI.Chat;


public class OpenAIService
{
    private readonly OpenAIClient _client;

  
    public OpenAIService()
    {
       
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
            throw new Exception("OpenAI API key not found in environment variables.");

        _client = new OpenAIClient(apiKey);
    }

    public async Task<string> DetectLanguageAsync(string text)
    {
        var chatClient = _client.GetChatClient("gpt-4.1-mini");

        var response = await chatClient.CompleteChatAsync(
            new List<ChatMessage>
            {
              ChatMessage.CreateSystemMessage("Detect the language of the user's message. Respond with only one word (e.g., English, German, French)."),
                ChatMessage.CreateUserMessage(text)
            }
        );

        var result = response.Value.Content[0].Text;

        return result.Trim();
    }
}