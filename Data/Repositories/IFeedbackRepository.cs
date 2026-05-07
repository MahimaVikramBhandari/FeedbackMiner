public interface IFeedbackRepository
{
    Task AddAsync(FeedbackItem item);
    Task<List<FeedbackItem>> GetRecentAsync(int take = 100);
    Task SaveChangesAsync();

    Task<FeedbackItem?> GetFeedbackById(Guid id);
}