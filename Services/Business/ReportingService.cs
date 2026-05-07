using System.Text.Json;

/// <summary>
/// Service for generating dashboard data and reports
/// </summary>
public class ReportingService
{
    private readonly FeedbackDbContext _dbContext;

    public ReportingService(FeedbackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get theme dashboard with top themes and recommendations
    /// </summary>
    public async Task<List<ThemeDashboardDto>> GetThemeDashboardAsync(int pageSize = 10)
    {
        var themes = _dbContext.Themes
            .OrderByDescending(t => t.ImpactScore)
            .Take(pageSize)
            .ToList();

        var result = new List<ThemeDashboardDto>();

        foreach (var theme in themes)
        {
            var recommendations = _dbContext.ActionRecommendations
                .Where(ar => ar.ThemeId == theme.Id)
                .OrderByDescending(ar => ar.ImpactScore)
                .Take(3)
                .ToList();

            var dto = new ThemeDashboardDto
            {
                ThemeId = theme.Id,
                Label = theme.Label,
                Description = theme.Description,
                FeedbackCount = theme.FeedbackCount,
                RelevanceScore = theme.RelevanceScore,
                ImpactScore = theme.ImpactScore,
                AverageSentimentScore = theme.AverageSentimentScore,
                AverageUrgencyScore = theme.AverageUrgencyScore,
                AffectedAreas = ParseJsonArray(theme.AffectedProductAreasJson),
                AffectedSegments = ParseJsonArray(theme.AffectedSegmentsJson),
                TopRecommendations = recommendations.Select(ar => new ActionRecommendationDto
                {
                    Id = ar.Id,
                    Title = ar.Title,
                    Description = ar.Description,
                    Category = ar.Category,
                    Priority = ar.Priority,
                    EstimatedEffort = ar.EstimatedEffort,
                    ImpactScore = ar.ImpactScore,
                    UsefulnessRating = ar.UsefulnessRating
                }).ToList(),
                CreatedAt = theme.CreatedAt,
                UpdatedAt = theme.UpdatedAt
            };

            result.Add(dto);
        }

        return result;
    }

    /// <summary>
    /// Get weekly digest of feedback activity
    /// </summary>
    public async Task<WeeklyDigestDto> GetWeeklyDigestAsync(DateTime? weekStart = null)
    {
        weekStart = weekStart ?? DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
        var weekEnd = weekStart.Value.AddDays(7);

        var weeklyFeedback = _dbContext.FeedbackItems
            .Where(f => f.CreatedOn >= weekStart && f.CreatedOn < weekEnd)
            .ToList();

        var newThemes = _dbContext.Themes
            .Where(t => t.CreatedAt >= weekStart && t.CreatedAt < weekEnd)
            .ToList();

        var highPriorityActions = _dbContext.ActionRecommendations
            .Where(ar => ar.Priority == "Critical" || ar.Priority == "High")
            .OrderByDescending(ar => ar.ImpactScore)
            .Take(5)
            .ToList();

        var topThemesByImpact = _dbContext.Themes
            .OrderByDescending(t => t.ImpactScore)
            .Take(5)
            .ToList();

        var digest = new WeeklyDigestDto
        {
            WeekStart = weekStart.Value,
            WeekEnd = weekEnd,
            TotalFeedbackReceived = weeklyFeedback.Count,
            NewThemesIdentified = newThemes.Count,
            ActiveThemes = _dbContext.Themes.Count(),
            AverageSentiment = weeklyFeedback.Average(f => f.SentimentScore ?? 0),
            CriticalUrgencyCount = weeklyFeedback.Count(f => f.UrgencyLevel == "Critical"),
            TopThemesByImpact = topThemesByImpact.Select(t => new ThemeDashboardDto
            {
                ThemeId = t.Id,
                Label = t.Label,
                Description = t.Description,
                FeedbackCount = t.FeedbackCount,
                RelevanceScore = t.RelevanceScore,
                ImpactScore = t.ImpactScore,
                AverageSentimentScore = t.AverageSentimentScore,
                AverageUrgencyScore = t.AverageUrgencyScore,
                AffectedAreas = ParseJsonArray(t.AffectedProductAreasJson),
                AffectedSegments = ParseJsonArray(t.AffectedSegmentsJson)
            }).ToList(),
            HighPriorityActions = highPriorityActions.Select(ar => new ActionRecommendationDto
            {
                Id = ar.Id,
                Title = ar.Title,
                Description = ar.Description,
                Category = ar.Category,
                Priority = ar.Priority,
                EstimatedEffort = ar.EstimatedEffort,
                ImpactScore = ar.ImpactScore,
                UsefulnessRating = ar.UsefulnessRating
            }).ToList(),
            FeedbackSourceBreakdown = weeklyFeedback
                .GroupBy(f => f.Source)
                .ToDictionary(g => g.Key, g => g.Count()),
            SentimentBreakdown = new Dictionary<string, int>
            {
                ["Positive"] = weeklyFeedback.Count(f => f.SentimentLabel == "Positive"),
                ["Negative"] = weeklyFeedback.Count(f => f.SentimentLabel == "Negative"),
                ["Neutral"] = weeklyFeedback.Count(f => f.SentimentLabel == "Neutral")
            }
        };

        return digest;
    }

    /// <summary>
    /// Export clusters for analysis
    /// </summary>
    public async Task<List<ThemeClusterExportDto>> ExportClustersAsync(Guid processingRunId)
    {
        var clusters = _dbContext.ThemeClusters
            .Where(tc => tc.ProcessingRunId == processingRunId)
            .ToList();

        var result = new List<ThemeClusterExportDto>();

        foreach (var cluster in clusters.OrderBy(c => c.ClusterNumber))
        {
            var feedbackItems = _dbContext.FeedbackItems
                .Where(f => f.ThemeClusterId == cluster.Id)
                .ToList();

            var exportDto = new ThemeClusterExportDto
            {
                ClusterNumber = cluster.ClusterNumber,
                SuggestedTheme = cluster.SuggestedTheme?.Label ?? "Unlabeled",
                ItemCount = cluster.ItemCount,
                AverageSimilarity = cluster.AverageSimilarity,
                SilhouetteScore = cluster.SilhouetteScore,
                FeedbackItems = feedbackItems.Select(f => new FeedbackSummaryDto
                {
                    Id = f.Id,
                    Text = f.ProcessedText ?? f.Text,
                    Source = f.Source,
                    SentimentScore = f.SentimentScore,
                    SentimentLabel = f.SentimentLabel,
                    UrgencyScore = f.UrgencyScore,
                    UrgencyLevel = f.UrgencyLevel,
                    CreatedAt = f.CreatedOn
                }).ToList()
            };

            result.Add(exportDto);
        }

        return result;
    }

    /// <summary>
    /// Get evaluation metrics for a processing run
    /// </summary>
    public async Task<ProcessingRunMetricsDto> GetProcessingRunMetricsAsync(Guid runId)
    {
        var run = _dbContext.ProcessingRuns.FirstOrDefault(pr => pr.Id == runId);
        if (run == null)
            return null;

        var duration = run.CompletedAt.HasValue
            ? run.CompletedAt.Value - run.StartedAt
            : (DateTime.UtcNow - run.StartedAt);

        return new ProcessingRunMetricsDto
        {
            RunId = run.Id,
            RunName = run.Name,
            FeedbackProcessed = run.FeedbackItemCount,
            ClustersCreated = run.ClusterCount,
            ThemesExtracted = run.ThemeCount,
            AverageClusterQuality = run.AverageClusterQuality,
            DuplicateDetectionPrecision = run.DuplicateDetectionPrecision,
            AverageThemeRelevance = run.AverageThemeRelevance,
            AverageActionUsefulness = run.AverageActionUsefulness,
            StartedAt = run.StartedAt,
            CompletedAt = run.CompletedAt,
            Status = run.Status,
            ProcessingDuration = duration
        };
    }

    /// <summary>
    /// Get all processing runs
    /// </summary>
    public async Task<List<ProcessingRunMetricsDto>> GetAllProcessingRunsAsync()
    {
        var runs = _dbContext.ProcessingRuns
            .OrderByDescending(pr => pr.StartedAt)
            .ToList();

        return runs.Select(run => new ProcessingRunMetricsDto
        {
            RunId = run.Id,
            RunName = run.Name,
            FeedbackProcessed = run.FeedbackItemCount,
            ClustersCreated = run.ClusterCount,
            ThemesExtracted = run.ThemeCount,
            AverageClusterQuality = run.AverageClusterQuality,
            DuplicateDetectionPrecision = run.DuplicateDetectionPrecision,
            AverageThemeRelevance = run.AverageThemeRelevance,
            AverageActionUsefulness = run.AverageActionUsefulness,
            StartedAt = run.StartedAt,
            CompletedAt = run.CompletedAt,
            Status = run.Status,
            ProcessingDuration = run.CompletedAt.HasValue
                ? run.CompletedAt.Value - run.StartedAt
                : (DateTime.UtcNow - run.StartedAt)
        }).ToList();
    }

    private List<string> ParseJsonArray(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
