

public class FeedbackService
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly TextProcessingPipeline _textProcessingPipeline;

    public FeedbackService(IFeedbackRepository repo, TextProcessingPipeline pipeline)
    {
        _feedbackRepository = repo;
        _textProcessingPipeline = pipeline;
    }

    public async Task IngestAsync(FeedbackItem item)
    {
        var result = await _textProcessingPipeline.ProcessAsync(item.Text);

        item.ProcessedText = result.CleanedText;
        item.Language = result.Language;
        item.CreatedOn = DateTime.UtcNow;

        await _feedbackRepository.AddAsync(item);
        await _feedbackRepository.SaveChangesAsync();
    }

    public async Task<FeedbackItem?> GetFeedbackById(Guid id)
    {
        return await _feedbackRepository.GetFeedbackById(id);
    }
}