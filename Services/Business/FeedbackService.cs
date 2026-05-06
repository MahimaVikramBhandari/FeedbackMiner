public class FeedbackService
{
    private readonly IFeedbackRepository _repo;
    private readonly TextProcessingPipeline _pipeline;

    public FeedbackService(IFeedbackRepository repo, TextProcessingPipeline pipeline)
    {
        _repo = repo;
        _pipeline = pipeline;
    }

    public async Task IngestAsync(FeedbackItem item)
    {
        var result = await _pipeline.ProcessAsync(item.Text);

        item.ProcessedText = result.CleanedText;
        item.Language = string.IsNullOrWhiteSpace(result.Language) ? "Unknown" : result.Language;

        await _repo.AddAsync(item);
        await _repo.SaveChangesAsync();
    }

    public Task<List<FeedbackItem>> GetRecentAsync(int take = 100)
    {
        return _repo.GetRecentAsync(take);
    }
}
