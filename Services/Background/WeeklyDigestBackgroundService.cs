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
                if (!stoppingToken.IsCancellationRequested)
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var digestService = scope.ServiceProvider.GetRequiredService<ScheduledDigestService>();
                        var weekStart = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek).Date;

                        await digestService.GenerateWeeklyDigestAsync(weekStart);
                        _logger.LogInformation("Weekly digest generated successfully");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during graceful shutdown
                _logger.LogInformation("Weekly Digest Background Service is shutting down");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in weekly digest background service: {ex.Message}");
            }

            // Check again after a short delay
            if (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Weekly Digest Background Service stopped");
    }

    private DateTime CalculateNextRunTime()
    {
        var now = DateTime.Now;

        // Calculate next Monday
        int daysUntilMonday =
            ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;

        var nextRun = now.Date
            .AddDays(daysUntilMonday)
            .Add(_scheduleTime);

        // If today's scheduled time already passed,
        // move to next Monday
        if (nextRun <= now)
        {
            nextRun = nextRun.AddDays(7);
        }

        return nextRun;
    }
}
