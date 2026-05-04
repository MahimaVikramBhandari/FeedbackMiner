using System.Text.Json;

/// <summary>
/// Service for generating scheduled weekly digests
/// </summary>
public class ScheduledDigestService
{
    private readonly ReportingService _reportingService;
    private readonly FeedbackDbContext _dbContext;
    private readonly ILogger<ScheduledDigestService> _logger;

    public ScheduledDigestService(
        ReportingService reportingService,
        FeedbackDbContext dbContext,
        ILogger<ScheduledDigestService> logger)
    {
        _reportingService = reportingService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Generate digest for a specific week
    /// </summary>
    public async Task<ScheduledDigestRun> GenerateWeeklyDigestAsync(DateTime weekStart, List<string> recipientEmails = null)
    {
        var weekEnd = weekStart.AddDays(7);

        var digestRun = new ScheduledDigestRun
        {
            Id = Guid.NewGuid(),
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation($"Generating digest for week {weekStart:yyyy-MM-dd} to {weekEnd:yyyy-MM-dd}");

            // Get weekly data
            var weeklyDigest = await _reportingService.GetWeeklyDigestAsync(weekStart);

            digestRun.FeedbackCount = weeklyDigest.TotalFeedbackReceived;
            digestRun.NewThemesCount = weeklyDigest.NewThemesIdentified;
            digestRun.CriticalActionItemsCount = (int)weeklyDigest.CriticalUrgencyCount;

            // Serialize digest content
            var digestContent = new
            {
                weeklyDigest.WeekStart,
                weeklyDigest.WeekEnd,
                weeklyDigest.TotalFeedbackReceived,
                weeklyDigest.NewThemesIdentified,
                weeklyDigest.ActiveThemes,
                weeklyDigest.AverageSentiment,
                weeklyDigest.CriticalUrgencyCount,
                TopThemesByImpact = weeklyDigest.TopThemesByImpact.Select(t => new
                {
                    t.Label,
                    t.FeedbackCount,
                    t.ImpactScore
                }),
                HighPriorityActions = weeklyDigest.HighPriorityActions.Select(a => new
                {
                    a.Title,
                    a.Priority,
                    a.Category
                }),
                weeklyDigest.FeedbackSourceBreakdown,
                weeklyDigest.ProductAreaBreakdown,
                weeklyDigest.SentimentBreakdown
            };

            digestRun.DigestContentJson = JsonSerializer.Serialize(digestContent);
            digestRun.RecipientEmailsJson = JsonSerializer.Serialize(recipientEmails ?? new List<string>());
            digestRun.Status = "Generated";
            digestRun.GeneratedAt = DateTime.UtcNow;

            _logger.LogInformation($"Digest generated successfully with {digestRun.FeedbackCount} feedback items");

            _dbContext.ScheduledDigestRuns.Add(digestRun);
            await _dbContext.SaveChangesAsync();

            return digestRun;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating digest: {ex.Message}");
            digestRun.Status = "Failed";
            digestRun.ErrorMessage = ex.Message;

            _dbContext.ScheduledDigestRuns.Add(digestRun);
            await _dbContext.SaveChangesAsync();

            throw;
        }
    }

    /// <summary>
    /// Get the next scheduled digest date (Monday morning)
    /// </summary>
    public DateTime GetNextDigestDate(TimeSpan? scheduleTime = null)
    {
        scheduleTime = scheduleTime ?? new TimeSpan(8, 0, 0); // Default 8 AM
        var today = DateTime.Now.Date;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;

        if (daysUntilMonday == 0 && DateTime.Now.TimeOfDay < scheduleTime)
            return today.Add(scheduleTime.Value);

        return today.AddDays(daysUntilMonday).Add(scheduleTime.Value);
    }

    /// <summary>
    /// Get digest for a specific period
    /// </summary>
    public async Task<ScheduledDigestRun> GetDigestForWeekAsync(DateTime weekStart)
    {
        var existingDigest = _dbContext.ScheduledDigestRuns
            .FirstOrDefault(d => d.WeekStart == weekStart);

        return existingDigest ?? await GenerateWeeklyDigestAsync(weekStart);
    }
}
