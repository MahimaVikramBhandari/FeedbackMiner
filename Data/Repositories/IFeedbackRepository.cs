public interface IFeedbackRepository
{
    Task AddAsync(FeedbackItem item);
    Task SaveChangesAsync();

    Task<FeedbackItem?> GetFeedbackById(Guid id);
}