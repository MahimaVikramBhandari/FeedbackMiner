public class TextProcessingPipeline
{
    private readonly List<ITextProcessor> _processors;

    public TextProcessingPipeline(List<ITextProcessor> processors)
    {
        _processors = processors;
    }

    public async Task<ProcessedResult> ProcessAsync(string input)
    {
        var context = new ProcessedResult
        {
            CleanedText = input
        };

        foreach (var processor in _processors)
        {
            await processor.ProcessAsync(context);
        }

        return context;
    }
}