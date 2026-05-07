using Microsoft.EntityFrameworkCore;
using System.Text.Json;

/// <summary>
/// Service for calculating and tracking evaluation metrics
/// </summary>
public class EvaluationMetricsService
{
    private readonly FeedbackDbContext _dbContext;
    private readonly ClusteringService _clusteringService;
    private readonly ActionRecommendationService _actionService;

    private const double RelevanceThreshold = 4.0;
    private const double UsefulnessThreshold = 4.0;
    private const double ClusteringPrecisionThreshold = 0.8;

    public EvaluationMetricsService(
        FeedbackDbContext dbContext,
        ClusteringService clusteringService,
        ActionRecommendationService actionService)
    {
        _dbContext = dbContext;
        _clusteringService = clusteringService;
        _actionService = actionService;
    }

    /// <summary>
    /// Run complete evaluation on a processing run
    /// </summary>
    public async Task<EvaluationRun> EvaluateProcessingRunAsync(Guid processingRunId)
    {
        var processingRun = _dbContext.ProcessingRuns
            .FirstOrDefault(pr => pr.Id == processingRunId);

        if (processingRun == null)
            throw new InvalidOperationException($"Processing run {processingRunId} not found");

        var evaluationRun = new EvaluationRun
        {
            Id = Guid.NewGuid(),
            ProcessingRunId = processingRunId,
            Status = "InProgress",
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            // Get themes from this processing run
            var themes = _dbContext.Themes
                .Where(t => t.ProcessingRunId == processingRunId)
                .ToList();

            // Get action recommendations from this processing run
            var actionRecommendations = _dbContext.ActionRecommendations
                .Where(ar => themes.Select(t => t.Id).Contains(ar.ThemeId))
                .ToList();

            // Get feedback items and clusters from this processing run
            var clusters = _dbContext.ThemeClusters
                .Where(tc => tc.ProcessingRunId == processingRunId)
                .Include(tc => tc.FeedbackItems)
                .ToList();

            // Evaluate themes
            await EvaluateThemesAsync(evaluationRun, themes);

            // Evaluate action recommendations
            await EvaluateActionRecommendationsAsync(evaluationRun, actionRecommendations);

            // Evaluate clustering quality
            EvaluateClusteringQuality(evaluationRun, clusters);

            // Calculate overall quality score
            CalculateOverallQualityScore(evaluationRun);

            evaluationRun.Status = "Completed";
            evaluationRun.CompletedAt = DateTime.UtcNow;
            evaluationRun.NotesJson = JsonSerializer.Serialize(new
            {
                ThemeCount = themes.Count,
                ActionRecommendationCount = actionRecommendations.Count,
                ClusterCount = clusters.Count
            });

            _dbContext.EvaluationRuns.Add(evaluationRun);
            await _dbContext.SaveChangesAsync();

            return evaluationRun;
        }
        catch (Exception ex)
        {
            evaluationRun.Status = "Failed";
            evaluationRun.ErrorMessage = ex.Message;
            evaluationRun.CompletedAt = DateTime.UtcNow;

            _dbContext.EvaluationRuns.Add(evaluationRun);
            await _dbContext.SaveChangesAsync();

            throw;
        }
    }

    private async Task EvaluateThemesAsync(EvaluationRun evaluationRun, List<Theme> themes)
    {
        int metCount = 0;
        double totalRelevance = 0;

        foreach (var theme in themes)
        {
            var themeEvaluation = new ThemeEvaluation
            {
                Id = Guid.NewGuid(),
                ThemeId = theme.Id,
                EvaluationRunId = evaluationRun.Id,
                RelevanceScore = theme.RelevanceScore,
                MetRelevanceThreshold = theme.RelevanceScore >= RelevanceThreshold,
                EstimatedAffectedCustomers = theme.FeedbackCount,
                FeedbackPercentage = theme.FeedbackCount > 0 ? (theme.FeedbackCount / (double)themes.Sum(t => t.FeedbackCount)) * 100 : 0,
                CreatedAt = DateTime.UtcNow,
                Trend = "Stable", // Placeholder - would calculate based on historical data
            };

            if (themeEvaluation.MetRelevanceThreshold)
                metCount++;

            totalRelevance += theme.RelevanceScore;

            evaluationRun.ThemeEvaluations.Add(themeEvaluation);
        }

        evaluationRun.AverageThemeRelevanceScore = themes.Count > 0 ? totalRelevance / themes.Count : 0;
        evaluationRun.ThemeRelevanceMetPercentage = themes.Count > 0 ? (metCount / (double)themes.Count) * 100 : 0;
    }

    private async Task EvaluateActionRecommendationsAsync(
        EvaluationRun evaluationRun,
        List<ActionRecommendation> actionRecommendations)
    {
        int metCount = 0;
        double totalUsefulness = 0;

        foreach (var recommendation in actionRecommendations)
        {
            // Get related feedback items for context
            var relatedItems = _dbContext.FeedbackItems
                .Where(f => f.ThemeId == recommendation.ThemeId)
                .ToList();

            // Evaluate usefulness
            var usefulnessScore = await _actionService.EvaluateUsefulnessAsync(recommendation, relatedItems);

            var areEvaluation = new ActionRecommendationEvaluation
            {
                Id = Guid.NewGuid(),
                ActionRecommendationId = recommendation.Id,
                EvaluationRunId = evaluationRun.Id,
                UsefulnessScore = usefulnessScore,
                MetUsefulnessThreshold = usefulnessScore >= UsefulnessThreshold,
                FeasibilityScore = CalculateFeasibility(recommendation),
                CreatedOn = DateTime.UtcNow,
                ExpectedImpact = recommendation.ImpactScore >= 4 ? "High" : (recommendation.ImpactScore >= 2 ? "Medium" : "Low"),
                EstimatedTimelineDays = recommendation.EstimatedEffort * 7
            };

            if (areEvaluation.MetUsefulnessThreshold)
                metCount++;

            totalUsefulness += usefulnessScore;

            // Update recommendation with usefulness rating
            recommendation.UsefulnessRating = usefulnessScore;

            evaluationRun.ActionRecommendationEvaluations.Add(areEvaluation);
        }

        evaluationRun.AverageRecommendationUsefulnessScore = actionRecommendations.Count > 0
            ? totalUsefulness / actionRecommendations.Count
            : 0;

        evaluationRun.RecommendationUsefulnessMetPercentage = actionRecommendations.Count > 0
            ? (metCount / (double)actionRecommendations.Count) * 100
            : 0;
    }

    private void EvaluateClusteringQuality(EvaluationRun evaluationRun, List<ThemeCluster> clusters)
    {
        if (clusters.Count == 0)
        {
            evaluationRun.ClusteringPrecision = 0;
            evaluationRun.DuplicateDetectionRate = 0;
            evaluationRun.AverageSilhouetteScore = 0;
            return;
        }

        // Calculate clustering precision based on silhouette scores
        double totalSilhouette = 0;
        int validClusters = 0;

        foreach (var cluster in clusters)
        {
            if (!string.IsNullOrEmpty(cluster.CentroidEmbeddingJson) && cluster.SilhouetteScore > 0)
            {
                totalSilhouette += cluster.SilhouetteScore;
                validClusters++;
            }
        }

        evaluationRun.AverageSilhouetteScore = validClusters > 0 ? totalSilhouette / validClusters : 0;

        // Clustering precision is based on average silhouette score
        // Map -1 to 1 range to 0 to 1 precision range
        evaluationRun.ClusteringPrecision = Math.Max(0, (evaluationRun.AverageSilhouetteScore + 1) / 2);

        // Duplicate detection rate - estimate based on cluster density
        int totalItems = clusters.Sum(c => c.ItemCount);
        int itemsInClusters = clusters.Count(c => c.ItemCount > 1);

        evaluationRun.DuplicateDetectionRate = totalItems > 0
            ? (itemsInClusters / (double)clusters.Count)
            : 0;
    }

    private double CalculateFeasibility(ActionRecommendation recommendation)
    {
        // Feasibility is inverse of effort - higher effort = lower feasibility
        // Effort is 1-5, convert to feasibility 1-5 (reverse)
        return 6 - recommendation.EstimatedEffort; // 5,4,3,2,1
    }

    private void CalculateOverallQualityScore(EvaluationRun evaluationRun)
    {
        // Weighted average: 40% relevance, 40% usefulness, 20% clustering
        double relevanceScore = Math.Min(evaluationRun.AverageThemeRelevanceScore / 5.0, 1.0) * 100;
        double usefulnessScore = Math.Min(evaluationRun.AverageRecommendationUsefulnessScore / 5.0, 1.0) * 100;
        double clusteringScore = evaluationRun.ClusteringPrecision * 100;

        evaluationRun.OverallQualityScore = (relevanceScore * 0.4) + (usefulnessScore * 0.4) + (clusteringScore * 0.2);
    }

    /// <summary>
    /// Get evaluation metrics summary for reporting
    /// </summary>
    public EvaluationMetricsSummary GetEvaluationSummary(EvaluationRun evaluationRun)
    {
        return new EvaluationMetricsSummary
        {
            EvaluationRunId = evaluationRun.Id,
            ProcessingRunId = evaluationRun.ProcessingRunId,
            EvaluatedAt = evaluationRun.CompletedAt ?? DateTime.UtcNow,
            AverageThemeRelevance = new MetricResult
            {
                Score = evaluationRun.AverageThemeRelevanceScore,
                Target = RelevanceThreshold,
                MetThreshold = evaluationRun.AverageThemeRelevanceScore >= RelevanceThreshold,
                MetPercentage = evaluationRun.ThemeRelevanceMetPercentage
            },
            ClusteringPrecision = new MetricResult
            {
                Score = evaluationRun.ClusteringPrecision,
                Target = ClusteringPrecisionThreshold,
                MetThreshold = evaluationRun.ClusteringPrecision >= ClusteringPrecisionThreshold,
                MetPercentage = (evaluationRun.ClusteringPrecision / ClusteringPrecisionThreshold) * 100
            },
            RecommendationUsefulness = new MetricResult
            {
                Score = evaluationRun.AverageRecommendationUsefulnessScore,
                Target = UsefulnessThreshold,
                MetThreshold = evaluationRun.AverageRecommendationUsefulnessScore >= UsefulnessThreshold,
                MetPercentage = evaluationRun.RecommendationUsefulnessMetPercentage
            },
            OverallQualityScore = evaluationRun.OverallQualityScore,
            Status = evaluationRun.Status
        };
    }
}

/// <summary>
/// Summary of evaluation metrics
/// </summary>
public class EvaluationMetricsSummary
{
    public Guid EvaluationRunId { get; set; }
    public Guid ProcessingRunId { get; set; }
    public DateTime EvaluatedAt { get; set; }

    public MetricResult AverageThemeRelevance { get; set; }
    public MetricResult ClusteringPrecision { get; set; }
    public MetricResult RecommendationUsefulness { get; set; }

    public double OverallQualityScore { get; set; }
    public string Status { get; set; }

    /// <summary>
    /// Get overall assessment
    /// </summary>
    public string GetAssessment()
    {
        if (!AverageThemeRelevance.MetThreshold)
            return "NEEDS IMPROVEMENT: Theme relevance below target (4.0/5)";

        if (!ClusteringPrecision.MetThreshold)
            return "NEEDS IMPROVEMENT: Clustering precision below target (0.8)";

        if (!RecommendationUsefulness.MetThreshold)
            return "NEEDS IMPROVEMENT: Recommendation usefulness below target (4.0/5)";

        return "APPROVED: All metrics meet quality thresholds";
    }
}

/// <summary>
/// Individual metric result
/// </summary>
public class MetricResult
{
    public double Score { get; set; }
    public double Target { get; set; }
    public bool MetThreshold { get; set; }
    public double MetPercentage { get; set; }
}
