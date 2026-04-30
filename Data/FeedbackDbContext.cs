using Microsoft.EntityFrameworkCore;

public class FeedbackDbContext : DbContext
{
    public FeedbackDbContext(DbContextOptions<FeedbackDbContext> options)
        : base(options) { }

    public DbSet<FeedbackItem> FeedbackItems { get; set; }
}