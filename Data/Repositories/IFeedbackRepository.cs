public interface IFeedbackRepository
{
    Task AddAsync(FeedbackItem item);
    Task SaveChangesAsync();
}