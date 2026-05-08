using System.Text.RegularExpressions;

public class PiiRedactor : ITextProcessor
{
    private readonly OpenAIService _openAIService;

    public PiiRedactor(OpenAIService openAIService)
    {
        _openAIService = openAIService;
    }

    public async Task ProcessAsync(ProcessedResult context)
    {
        var text = context.CleanedText;

        if (string.IsNullOrWhiteSpace(text))
        {
            context.CleanedText = string.Empty;
            return;
        }

        try
        {
            text = await _openAIService.RedactSensitiveTextAsync(text);
        }
        catch
        {
            // Continue with regex fallback masking.
        }

        text = Regex.Replace(text,
            @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
            "****");

        text = Regex.Replace(text,
            @"\+?\d[\d\s\-]{7,}\d",
            "****");

        text = Regex.Replace(
            text,
            @"\b(my name is|i am|i'm|this is)\s+([\p{L}]+(?:\s+[\p{L}]+){0,2})\b",
            "$1 ****",
            RegexOptions.IgnoreCase);

        text = Regex.Replace(
            text,
            @"\b(account|customer|ticket|order|id)\s*[:#-]?\s*[A-Za-z0-9\-]{4,}\b",
            "$1 ****",
            RegexOptions.IgnoreCase);

        context.CleanedText = text;
    }
}
