using System.Text.RegularExpressions;

public class PiiRedactor : ITextProcessor
{
    private static readonly List<(Regex regex, string replacement)> Rules = new()
    {
        (
            new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled),
            "[REDACTED_EMAIL]"
        ),

        (
            new Regex(@"\+?\d[\d\s\-]{7,}\d", RegexOptions.Compiled),
            "[REDACTED_PHONE]"
        ),

        (
            new Regex(@"\b(?:\d[ -]*?){13,16}\b", RegexOptions.Compiled),
            "[REDACTED_CARD]"
        ),

        (
            new Regex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.Compiled),
            "[REDACTED_IP]"
        ),

        (
            new Regex(@"\b[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}\b", RegexOptions.Compiled),
            "[REDACTED_GUID]"
        ),

        (
            new Regex(@"\b(customer|account|ticket|order)[\s\-_:#]*\d+\b",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "[REDACTED_ID]"
        )
    };

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

        text = Regex.Replace(
            text,
            @"\b(account|customer|ticket|order|id)\s*[:#-]?\s*[A-Za-z0-9\-]{4,}\b",
            "$1 [REDACTED_ID]",
            RegexOptions.IgnoreCase);

        context.CleanedText = text;
        return Task.CompletedTask;
    }
}
