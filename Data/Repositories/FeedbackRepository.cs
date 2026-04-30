public class FeedbackRepository : IFeedbackRepository
{
    private readonly FeedbackDbContext _context;

    public FeedbackRepository(FeedbackDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FeedbackItem item)
    {
        await _context.FeedbackItems.AddAsync(item);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}