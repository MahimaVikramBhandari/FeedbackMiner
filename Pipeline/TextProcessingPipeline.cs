public class TextProcessingPipeline
{
    private readonly List<ITextProcessor> _processors;

    public TextProcessingPipeline(IEnumerable<ITextProcessor> processors)
    {
        _processors = processors.ToList();
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
