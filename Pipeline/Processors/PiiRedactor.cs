using System.Text.RegularExpressions;

public class PiiRedactor : ITextProcessor
{
    public Task ProcessAsync(ProcessedResult context)
    {
        if (string.IsNullOrWhiteSpace(context.CleanedText))
            return Task.CompletedTask;

        var text = context.CleanedText;

        if (string.IsNullOrWhiteSpace(text))
        {
            context.CleanedText = string.Empty;
            return Task.CompletedTask;
        }

        text = Regex.Replace(text,
            @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
            "[REDACTED_EMAIL]");

        text = Regex.Replace(text,
            @"\+?\d[\d\s\-]{7,}\d",
            "[REDACTED_PHONE]");

        text = Regex.Replace(
            text,
            @"\b(my name is|name is)\s+([\p{L}]+(?:\s+[\p{L}]+){0,2})\b",
            "$1 [REDACTED_NAME]",
            RegexOptions.IgnoreCase);

        text = Regex.Replace(text,
            @"\b(?:\d[ -]*?){13,16}\b",
            "[REDACTED_CARD]");

        text = Regex.Replace(text,
            @"\b(?:\d{1,3}\.){3}\d{1,3}\b", "[REDACTED_IP]");

        context.CleanedText = text;
        return Task.CompletedTask;
    }
}
