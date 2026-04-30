using System.Text.RegularExpressions;

public class PiiRedactor : ITextProcessor
{
    public Task ProcessAsync(ProcessedResult context)
    {
        var text = context.CleanedText;

        text = Regex.Replace(text,
            @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
            "[REDACTED_EMAIL]");

        text = Regex.Replace(text,
            @"\+?\d[\d\s\-]{7,}\d",
            "[REDACTED_PHONE]");

        context.CleanedText = text;

        return Task.CompletedTask;
    }
}