

public class LanguageDetector : ITextProcessor
{
    private readonly OpenAIService _openAI;

    public LanguageDetector(OpenAIService openAI)
    {
        _openAI = openAI;
    }

    public async Task ProcessAsync(ProcessedResult context)
    {
        var lang = await _openAI.DetectLanguageAsync(context.CleanedText);
        context.Language = lang;
    }
}