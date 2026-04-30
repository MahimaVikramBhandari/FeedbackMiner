public class TextCleaner : ITextProcessor
{
    public Task ProcessAsync(ProcessedResult context)
    {
        context.CleanedText = context.CleanedText
            ?.Replace("\n", " ")
            ?.Replace("\r", " ")
            ?.Trim();

        return Task.CompletedTask;
    }
}