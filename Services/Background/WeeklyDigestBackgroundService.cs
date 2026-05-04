using Microsoft.Extensions.Hosting;

/// <summary>
/// Background service for automatically generating weekly digests
/// </summary>
public class WeeklyDigestBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WeeklyDigestBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    // Default to Monday at 8 AM
    private TimeSpan _scheduleTime = new TimeSpan(8, 0, 0);

    public WeeklyDigestBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<WeeklyDigestBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;

        // Try to read schedule time from configuration
        if (TimeSpan.TryParse(_configuration["FeedbackMiner:DigestScheduleTime"] ?? "08:00:00", out var time))
            _scheduleTime = time;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Weekly Digest Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextRun = CalculateNextRunTime();
                var delay = nextRun - DateTime.Now;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation($"Next digest generation scheduled for {nextRun:yyyy-MM-dd HH:mm:ss}");
                    await Task.Delay(delay, stoppingToken);
                }

                // Generate digest for last week
                using (var scope = _serviceProvider.CreateScope())
                {
                    var digestService = scope.ServiceProvider.GetRequiredService<ScheduledDigestService>();
                    var weekStart = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek).Date;

                    await digestService.GenerateWeeklyDigestAsync(weekStart);
                    _logger.LogInformation("Weekly digest generated successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in weekly digest background service: {ex.Message}");
            }

            // Check again after a short delay
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        _logger.LogInformation("Weekly Digest Background Service stopped");
    }

    private DateTime CalculateNextRunTime()
    {
        var today = DateTime.Now.Date;
        var nextMonday = today.AddDays((DayOfWeek.Monday - today.DayOfWeek + 7) % 7);

        if (nextMonday <= today)
            nextMonday = nextMonday.AddDays(7);

        return nextMonday.Add(_scheduleTime);
    }
}
