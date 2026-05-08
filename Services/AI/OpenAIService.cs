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

    public async Task<string> RedactSensitiveTextAsync(string text)
    {
        var chatClient = _client.GetChatClient("gpt-4.1-mini");

        var response = await chatClient.CompleteChatAsync(
            new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(
                    "Redact personally identifiable information from user text. " +
                    "Replace names with [REDACTED_NAME], phones with [REDACTED_PHONE], emails with [REDACTED_EMAIL], " +
                    "addresses with [REDACTED_ADDRESS], and account identifiers with [REDACTED_ID]. " +
                    "Return only the redacted text."),
                ChatMessage.CreateUserMessage(text)
            }
        );

        return response.Value.Content[0].Text.Trim();
    }

    public async Task<string> AskDashboardAssistantAsync(string userQuestion)
    {
        var chatClient = _client.GetChatClient("gpt-4.1-mini");

        var systemPrompt =
            "You are FeedbackMiner Dashboard Assistant. " +
            "Answer only questions related to FeedbackMiner and customer feedback theme mining workflows. " +
            "Allowed topics: CSAT/NPS comments, support feedback, product feature list, escalation categories, " +
            "embedding-based clustering, GPT theme labeling, sentiment/urgency extraction, " +
            "evaluation thresholds (theme relevance >= 4/5, duplicate clustering precision >= 0.8, action usefulness >= 4/5), " +
            "and deliverables (theme dashboard, weekly digest, cluster export, evaluation notebook). " +
            "Also guide users on which app page to use (Dashboard, Feedback, Themes, Reports) and what to expect there. " +
            "If question is outside scope, politely refuse and redirect to one of the allowed topics in 1-2 sentences. " +
            "Keep answers concise and practical.";

        var response = await chatClient.CompleteChatAsync(
            new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(userQuestion)
            }
        );

        return response.Value.Content[0].Text.Trim();
    }
}
