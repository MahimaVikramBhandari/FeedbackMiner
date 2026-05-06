using Microsoft.EntityFrameworkCore;

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

    public async Task<List<FeedbackItem>> GetRecentAsync(int take = 100)
    {
        return await _context.FeedbackItems
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
