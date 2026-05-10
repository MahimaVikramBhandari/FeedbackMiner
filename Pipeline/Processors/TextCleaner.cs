using System.Text.RegularExpressions;

public class TextCleaner : ITextProcessor
{
    public Task ProcessAsync(ProcessedResult context)
    {
        var cleaned = context.CleanedText
            ?.Replace("\n", " ")
            ?.Replace("\r", " ")
            ?.Replace("\t", " ")
            ?.Trim();

        if (!string.IsNullOrWhiteSpace(cleaned))
        {
            cleaned = Regex.Replace(cleaned, @"[^\p{L}\p{M}\p{N}\s\.,!\?:;'\-_()/@]", " ");
            cleaned = Regex.Replace(cleaned, @"\s{2,}", " ").Trim();
        }

        context.CleanedText = cleaned ?? string.Empty;
        return Task.CompletedTask;
    }
}
