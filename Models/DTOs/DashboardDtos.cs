/// <summary>
/// Dashboard data for displaying theme overview
/// </summary>
public class ThemeDashboardDto
{
    public Guid ThemeId { get; set; }
    public string Label { get; set; }
    public string Description { get; set; }
    public int FeedbackCount { get; set; }
    public double RelevanceScore { get; set; }
    public double ImpactScore { get; set; }
    public double AverageSentimentScore { get; set; }
    public double AverageUrgencyScore { get; set; }
    public List<string> AffectedAreas { get; set; }
    public List<string> AffectedSegments { get; set; }
    public List<ActionRecommendationDto> TopRecommendations { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Action recommendation data
/// </summary>
public class ActionRecommendationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string Priority { get; set; }
    public int EstimatedEffort { get; set; }
    public double ImpactScore { get; set; }
    public double? UsefulnessRating { get; set; }
    public string Status { get; set; }
}

/// <summary>
/// Cluster data for export
/// </summary>
public class ThemeClusterExportDto
{
    public int ClusterNumber { get; set; }
    public string SuggestedTheme { get; set; }
    public int ItemCount { get; set; }
    public double AverageSimilarity { get; set; }
    public double SilhouetteScore { get; set; }
    public List<FeedbackSummaryDto> FeedbackItems { get; set; }
}

/// <summary>
/// Summary of feedback item
/// </summary>
public class FeedbackSummaryDto
{
    public Guid Id { get; set; }
    public string Text { get; set; }
    public string Source { get; set; }
    public int? Rating { get; set; }
    public double? SentimentScore { get; set; }
    public string SentimentLabel { get; set; }
    public double? UrgencyScore { get; set; }
    public string UrgencyLevel { get; set; }
    public string ProductArea { get; set; }
    public string Category { get; set; }
    public string CustomerSegment { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Weekly feedback digest
/// </summary>
public class WeeklyDigestDto
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public int TotalFeedbackReceived { get; set; }
    public int NewThemesIdentified { get; set; }
    public int ActiveThemes { get; set; }
    public double AverageSentiment { get; set; }
    public double CriticalUrgencyCount { get; set; }
    public List<ThemeDashboardDto> TopThemesByImpact { get; set; }
    public List<ActionRecommendationDto> HighPriorityActions { get; set; }
    public Dictionary<string, int> FeedbackSourceBreakdown { get; set; }
    public Dictionary<string, int> ProductAreaBreakdown { get; set; }
    public Dictionary<string, int> SentimentBreakdown { get; set; }
}

/// <summary>
/// Processing run metrics
/// </summary>
public class ProcessingRunMetricsDto
{
    public Guid RunId { get; set; }
    public string RunName { get; set; }
    public int FeedbackProcessed { get; set; }
    public int ClustersCreated { get; set; }
    public int ThemesExtracted { get; set; }
    public double AverageClusterQuality { get; set; }
    public double DuplicateDetectionPrecision { get; set; }
    public double AverageThemeRelevance { get; set; }
    public double AverageActionUsefulness { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; }
    public TimeSpan? ProcessingDuration { get; set; }
}
