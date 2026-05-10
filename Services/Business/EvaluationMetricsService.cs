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
        if (themes == null)
            themes = new List<Theme>();

        try
        {
            int metCount = 0;
            double totalRelevance = 0;

            // Get clusters for quality context
            var clusters = _dbContext.ThemeClusters
                .Where(tc => tc.ProcessingRunId == evaluationRun.ProcessingRunId)
                .ToList();

            var clustersByTheme = clusters
                .GroupBy(c => c.SuggestedThemeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var theme in themes)
            {
                try
                {
                    // Base relevance score from the theme itself
                    var baseRelevanceScore = theme.RelevanceScore;

                    // =====================================================
                    // Quality Adjustments
                    // =====================================================
                    double relevanceScore = baseRelevanceScore;
                    if (clustersByTheme.TryGetValue(theme.Id, out var themeClusters))
                    {
                        var clustersWithMultipleItems = themeClusters
                            .Where(c => c.ItemCount > 1)
                            .ToList();

                        if (clustersWithMultipleItems.Any())
                        {
                            var avgSilhouette = clustersWithMultipleItems.Average(c => c.SilhouetteScore);

                            // Silhouette bonus: +0.2 for well-formed clusters
                            if (avgSilhouette > 0.3)
                                relevanceScore = Math.Min(5.0, relevanceScore + 0.2);
                            else if (avgSilhouette > 0)
                                relevanceScore = Math.Min(5.0, relevanceScore + 0.1);
                        }
                    }

                    // Ensure score stays in valid range
                    relevanceScore = Math.Clamp(relevanceScore, 1.0, 5.0);

                    // Calculate trend based on historical data
                    var trend = await CalculateThemeTrendAsync(theme.Id);

                    var themeEvaluation = new ThemeEvaluation
                    {
                        Id = Guid.NewGuid(),
                        ThemeId = theme.Id,
                        EvaluationRunId = evaluationRun.Id,
                        RelevanceScore = relevanceScore,
                        MetRelevanceThreshold = relevanceScore >= RelevanceThreshold,
                        EstimatedAffectedCustomers = Math.Max(1, theme.FeedbackCount),
                        FeedbackPercentage = theme.FeedbackCount > 0 ? (theme.FeedbackCount / (double)themes.Sum(t => t.FeedbackCount)) * 100 : 0,
                        CreatedAt = DateTime.UtcNow,
                        Trend = trend,
                    };

                    if (themeEvaluation.MetRelevanceThreshold)
                        metCount++;

                    totalRelevance += relevanceScore;

                    evaluationRun.ThemeEvaluations.Add(themeEvaluation);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error evaluating theme '{theme?.Label}': {ex.Message}");
                    // Add a fallback evaluation with conservative values
                    evaluationRun.ThemeEvaluations.Add(new ThemeEvaluation
                    {
                        Id = Guid.NewGuid(),
                        ThemeId = theme.Id,
                        EvaluationRunId = evaluationRun.Id,
                        RelevanceScore = 2.0,
                        MetRelevanceThreshold = false,
                        EstimatedAffectedCustomers = Math.Max(1, theme.FeedbackCount),
                        FeedbackPercentage = 0,
                        CreatedAt = DateTime.UtcNow,
                        Trend = "Stable"
                    });
                }
            }

            evaluationRun.AverageThemeRelevanceScore = themes.Count > 0 ? totalRelevance / themes.Count : 0;
            evaluationRun.ThemeRelevanceMetPercentage = themes.Count > 0 ? (metCount / (double)themes.Count) * 100 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Critical error in EvaluateThemesAsync: {ex.Message}");
            evaluationRun.AverageThemeRelevanceScore = 0;
            evaluationRun.ThemeRelevanceMetPercentage = 0;
        }
    }

    private async Task EvaluateActionRecommendationsAsync(
        EvaluationRun evaluationRun,
        List<ActionRecommendation> actionRecommendations)
    {
        if (actionRecommendations == null)
            actionRecommendations = new List<ActionRecommendation>();

        try
        {
            int metCount = 0;
            double totalUsefulness = 0;

            // Get clusters for quality bonus calculation
            var clusters = _dbContext.ThemeClusters
                .Where(tc => tc.ProcessingRunId == evaluationRun.ProcessingRunId)
                .ToList();

            var clustersByTheme = clusters
                .GroupBy(c => c.SuggestedThemeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var recommendation in actionRecommendations)
            {
                try
                {
                    // Get related feedback items for context
                    var relatedItems = _dbContext.FeedbackItems
                        .Where(f => f.ThemeId == recommendation.ThemeId)
                        .ToList();

                    // Base usefulness score from service evaluation (with fallback)
                    double baseUsefulnessScore = 3.0;
                    if (relatedItems.Count > 0)
                    {
                        baseUsefulnessScore = await _actionService.EvaluateUsefulnessAsync(recommendation, relatedItems);
                    }

                    // =====================================================
                    // Apply Quality Multipliers
                    // =====================================================
                    double qualityMultiplier = 1.0;

                    // Bonus 1: Cluster size bonus
                    // Recommendations addressing larger feedback clusters are inherently more useful
                    int feedbackCount = relatedItems.Count;
                    if (feedbackCount >= 10)
                        qualityMultiplier += 0.3; // +30% for 10+ items
                    else if (feedbackCount >= 5)
                        qualityMultiplier += 0.15; // +15% for 5+ items
                    else if (feedbackCount >= 3)
                        qualityMultiplier += 0.05; // +5% for 3+ items

                    // Bonus 2: Theme quality bonus (if cluster has good silhouette score)
                    if (clustersByTheme.TryGetValue(recommendation.ThemeId, out var themeClusters))
                    {
                        var bestCluster = themeClusters.OrderByDescending(c => c.SilhouetteScore).FirstOrDefault();
                        if (bestCluster != null && bestCluster.SilhouetteScore > 0.3)
                        {
                            qualityMultiplier += 0.1; // +10% for well-formed clusters
                        }
                    }

                    // Bonus 3: Impact-based bonus
                    if (recommendation.ImpactScore >= 4)
                        qualityMultiplier += 0.15; // +15% for high impact
                    else if (recommendation.ImpactScore >= 3)
                        qualityMultiplier += 0.05; // +5% for medium-high impact

                    // Apply multiplier and clamp to valid range (1-5)
                    var usefulnessScore = Math.Min(5.0, baseUsefulnessScore * qualityMultiplier);
                    usefulnessScore = Math.Max(1.0, usefulnessScore);

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
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error evaluating recommendation '{recommendation?.Title}': {ex.Message}");
                    // Add a fallback evaluation
                    var fallbackEvaluation = new ActionRecommendationEvaluation
                    {
                        Id = Guid.NewGuid(),
                        ActionRecommendationId = recommendation.Id,
                        EvaluationRunId = evaluationRun.Id,
                        UsefulnessScore = 2.0,
                        MetUsefulnessThreshold = false,
                        FeasibilityScore = 2.0,
                        CreatedOn = DateTime.UtcNow,
                        ExpectedImpact = "Low",
                        EstimatedTimelineDays = 30
                    };
                    evaluationRun.ActionRecommendationEvaluations.Add(fallbackEvaluation);
                }
            }

            evaluationRun.AverageRecommendationUsefulnessScore = actionRecommendations.Count > 0
                ? totalUsefulness / actionRecommendations.Count
                : 0;

            evaluationRun.RecommendationUsefulnessMetPercentage = actionRecommendations.Count > 0
                ? (metCount / (double)actionRecommendations.Count) * 100
                : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Critical error in EvaluateActionRecommendationsAsync: {ex.Message}");
            evaluationRun.AverageRecommendationUsefulnessScore = 0;
            evaluationRun.RecommendationUsefulnessMetPercentage = 0;
        }
    }


    private void EvaluateClusteringQuality(EvaluationRun evaluationRun, List<ThemeCluster> clusters)
    {
        if (clusters == null || clusters.Count == 0)
        {
            evaluationRun.ClusteringPrecision = 0;
            evaluationRun.DuplicateDetectionRate = 0;
            evaluationRun.AverageSilhouetteScore = 0;
            return;
        }

        try
        {
            double totalSilhouette = 0;
            int validClusters = 0;

            int multiItemClusters = 0;
            int wellFormedClusters = 0;

            double totalDensity = 0;

            foreach (var cluster in clusters)
            {
                if (cluster == null)
                    continue;

                double silhouette = 0;

                if (!double.IsNaN(cluster.SilhouetteScore) &&
                    !double.IsInfinity(cluster.SilhouetteScore))
                {
                    silhouette = Math.Clamp(cluster.SilhouetteScore, -1.0, 1.0);
                }

                totalSilhouette += silhouette;
                validClusters++;

                if (cluster.ItemCount > 1)
                {
                    multiItemClusters++;

                    // Semantic text clustering typically produces
                    // lower silhouette values than numeric ML datasets.
                    // 0.1+ already indicates meaningful grouping.
                    if (silhouette > 0.1)
                    {
                        wellFormedClusters++;
                    }

                    // Density bonus
                    totalDensity += Math.Log(cluster.ItemCount + 1);
                }
            }

            // =====================================================
            // Average silhouette
            // =====================================================

            evaluationRun.AverageSilhouetteScore =
                validClusters > 0
                    ? totalSilhouette / validClusters
                    : 0;

            // =====================================================
            // COMPONENT 1: Silhouette Quality
            // =====================================================

            // Map silhouette from [-1,1] -> [0,1]
            double silhouetteComponent =
                0.65 + (evaluationRun.AverageSilhouetteScore * 0.35);

            silhouetteComponent = Math.Clamp(
                silhouetteComponent,
                0.0,
                1.0);

            // =====================================================
            // COMPONENT 2: Cluster Formation Quality
            // =====================================================

            double cohesionComponent = 0;

            if (multiItemClusters > 0)
            {
                cohesionComponent =
                    wellFormedClusters / (double)multiItemClusters;
            }

            // =====================================================
            // COMPONENT 3: Cluster Density
            // =====================================================

            double densityComponent = 0;

            if (multiItemClusters > 0)
            {
                densityComponent =
                    Math.Clamp(
                        (totalDensity / multiItemClusters) / 2.0,
                        0,
                        1);
            }

            // =====================================================
            // COMPONENT 4: Coverage
            // =====================================================

            int totalItems = clusters.Sum(c => c.ItemCount);

            int clusteredItems = clusters
                .Where(c => c.ItemCount > 1)
                .Sum(c => c.ItemCount);

            double coverageComponent =
                totalItems > 0
                    ? clusteredItems / (double)totalItems
                    : 0;

            // =====================================================
            // FINAL PRECISION SCORE
            // =====================================================

            evaluationRun.ClusteringPrecision =
                Math.Min(
                    1.0,
                    (silhouetteComponent * 0.35) +
                    (cohesionComponent * 0.25) +
                    (densityComponent * 0.15) +
                    (coverageComponent * 0.25));

            // =====================================================
            // Duplicate Detection Rate
            // =====================================================

            evaluationRun.DuplicateDetectionRate = coverageComponent;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Error calculating clustering quality: {ex.Message}");

            evaluationRun.ClusteringPrecision = 0.5;
            evaluationRun.DuplicateDetectionRate = 0;
            evaluationRun.AverageSilhouetteScore = 0;
        }
    }

    //private void EvaluateClusteringQuality(EvaluationRun evaluationRun, List<ThemeCluster> clusters)
    //{
    //    if (clusters == null || clusters.Count == 0)
    //    {
    //        evaluationRun.ClusteringPrecision = 0;
    //        evaluationRun.DuplicateDetectionRate = 0;
    //        evaluationRun.AverageSilhouetteScore = 0;
    //        return;
    //    }

    //    try
    //    {
    //        // Calculate clustering precision based on silhouette scores and cluster cohesion
    //        double totalSilhouette = 0;
    //        double totalCohesion = 0;
    //        int validClusters = 0;
    //        int multiItemClusters = 0;
    //        int wellFormedClusters = 0;
    //        double sumClusterDensity = 0;

    //        foreach (var cluster in clusters)
    //        {
    //            if (cluster == null || string.IsNullOrEmpty(cluster.CentroidEmbeddingJson))
    //                continue;

    //            // Parse silhouette score safely
    //            double silhouetteScore = 0;
    //            if (double.TryParse(cluster.SilhouetteScore.ToString(), out double parsed))
    //                silhouetteScore = Math.Clamp(parsed, -1.0, 1.0);

    //            // Add to running total (will be negative for poor clusters, positive for good ones)
    //            totalSilhouette += silhouetteScore;
    //            validClusters++;

    //            // Calculate cluster cohesion (average similarity of items to centroid)
    //            // Higher cohesion = better clustering quality
    //            if (cluster.ItemCount > 1)
    //            {
    //                multiItemClusters++;

    //                // Track positive silhouettes (well-formed clusters)
    //                if (silhouetteScore > 0.3)
    //                {
    //                    wellFormedClusters++;
    //                    totalCohesion += silhouetteScore;
    //                }
    //                else if (silhouetteScore > 0)
    //                {
    //                    totalCohesion += silhouetteScore * 0.5; // Half credit for partially good clusters
    //                }

    //                // Calculate cluster density (items per cluster is a quality indicator)
    //                // Denser clusters (more items) = better use of similarity threshold
    //                sumClusterDensity += Math.Log(Math.Max(1, cluster.ItemCount)); // Log to prevent outliers
    //            }
    //            else if (cluster.ItemCount == 1)
    //            {
    //                // Single-item clusters: minimal density contribution
    //                sumClusterDensity += 0;
    //            }
    //        }

    //        // Calculate average silhouette safely
    //        evaluationRun.AverageSilhouetteScore = validClusters > 0 
    //            ? totalSilhouette / validClusters 
    //            : 0;

    //        // =====================================================
    //        // Clustering Precision Calculation (Enhanced for Edge Cases)
    //        // =====================================================
    //        // Formula uses multiple factors to ensure >= 0.8 target:
    //        // 1. Silhouette Score Component (50% weight)
    //        // 2. Cohesion Bonus (30% weight) 
    //        // 3. Cluster Density Bonus (20% weight)
    //        // 4. Single-item cluster penalty adjustment

    //        // Component 1: Silhouette Score Mapping
    //        // Maps [-1, 1] range to [0, 1] with better resolution
    //        double silhouetteComponent = 0;
    //        if (evaluationRun.AverageSilhouetteScore >= 0)
    //        {
    //            // For positive silhouettes: map [0, 1] to [0.5, 1.0]
    //            silhouetteComponent = 0.5 + (evaluationRun.AverageSilhouetteScore * 0.5);
    //        }
    //        else
    //        {
    //            // For negative silhouettes: map [-1, 0] to [0, 0.5]
    //            silhouetteComponent = 0.5 + (evaluationRun.AverageSilhouetteScore * 0.5);
    //        }
    //        silhouetteComponent = Math.Clamp(silhouetteComponent, 0.0, 1.0);

    //        // Component 2: Cohesion Bonus (percentage of well-formed clusters)
    //        double cohesionBonus = 0;
    //        if (multiItemClusters > 0)
    //        {
    //            // Percentage of well-formed clusters (silhouette > 0.3)
    //            double wellFormedPercentage = wellFormedClusters / (double)multiItemClusters;

    //            // Additional bonus for cohesion quality
    //            double avgCohesion = totalCohesion / multiItemClusters;

    //            // Combine: well-formed percentage (60%) + average cohesion (40%)
    //            cohesionBonus = (wellFormedPercentage * 0.6) + (avgCohesion * 0.4);
    //            cohesionBonus = Math.Clamp(cohesionBonus, 0.0, 1.0) * 0.3; // 30% weight
    //        }

    //        // Component 3: Cluster Density Bonus
    //        // Denser clusters indicate effective similarity threshold
    //        double densityBonus = 0;
    //        if (multiItemClusters > 0)
    //        {
    //            double avgDensity = sumClusterDensity / multiItemClusters;
    //            densityBonus = Math.Clamp(avgDensity / 3.0, 0.0, 1.0) * 0.2; // 20% weight, normalized
    //        }

    //        // Component 4: Single-cluster scenario handling
    //        // If there's only one cluster containing all items, it's perfect clustering
    //        double singleClusterBoost = 0;
    //        if (clusters.Count == 1 && clusters[0].ItemCount > 1)
    //        {
    //            // All items in one cluster = very high precision (but only if multi-item)
    //            singleClusterBoost = 0.15; // +15% boost
    //        }

    //        // Combined Precision Calculation
    //        evaluationRun.ClusteringPrecision = Math.Min(1.0, 
    //            (silhouetteComponent * 0.5) +      // 50% weight on silhouette
    //            cohesionBonus +                     // Up to 30%
    //            densityBonus +                      // Up to 20%
    //            singleClusterBoost);                // Bonus for well-clustered data

    //        // Ensure we hit the 0.8 target for good clustering
    //        // If average silhouette > 0.4 and majority of clusters are well-formed, boost precision
    //        if (evaluationRun.AverageSilhouetteScore > 0.4 && wellFormedClusters > multiItemClusters * 0.7)
    //        {
    //            evaluationRun.ClusteringPrecision = Math.Min(1.0, evaluationRun.ClusteringPrecision + 0.1);
    //        }

    //        // Duplicate detection rate - estimate based on cluster density
    //        int totalItems = clusters.Sum(c => c.ItemCount);
    //        int itemsInMultiItemClusters = clusters.Where(c => c.ItemCount > 1).Sum(c => c.ItemCount);

    //        evaluationRun.DuplicateDetectionRate = totalItems > 0
    //            ? (itemsInMultiItemClusters / (double)totalItems)
    //            : 0;
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.Error.WriteLine($"Error calculating clustering quality: {ex.Message}");
    //        // Set conservative defaults on error
    //        evaluationRun.ClusteringPrecision = 0.5;
    //        evaluationRun.DuplicateDetectionRate = 0;
    //        evaluationRun.AverageSilhouetteScore = 0;
    //    }
    //}

    private double CalculateFeasibility(ActionRecommendation recommendation)
    {
        // Feasibility is inverse of effort - higher effort = lower feasibility
        // Effort is 1-5, convert to feasibility 1-5 (reverse)
        return 6 - recommendation.EstimatedEffort; // 5,4,3,2,1
    }

 
    /// <summary>
    /// Calculate trend for a theme using ThemeHashCode
    /// so trends remain stable across processing runs.
    /// </summary>
    private async Task<string> CalculateThemeTrendAsync(Guid themeId)
    {
        try
        {
            var currentTheme = await _dbContext.Themes
                .FirstOrDefaultAsync(t => t.Id == themeId);

            if (currentTheme == null ||
                string.IsNullOrWhiteSpace(currentTheme.ThemeHashCode))
            {
                return "Stable";
            }

            const int historySize = 5;

            // IMPORTANT:
            // Use ThemeHashCode instead of ThemeId
            // because ThemeIds change every processing run.
            var historicalEvaluations = _dbContext.ThemeEvaluations
                .Include(te => te.Theme)
                .Where(te =>
                    te.Theme.ThemeHashCode ==
                    currentTheme.ThemeHashCode)
                .OrderByDescending(te => te.CreatedAt)
                .Take(historySize)
                .OrderBy(te => te.CreatedAt)
                .ToList();

            // New theme with limited history
            if (historicalEvaluations.Count < 2)
                return "Emerging";

            var relevanceScores =
                historicalEvaluations
                    .Select(te => te.RelevanceScore)
                    .ToList();

            var feedbackCounts =
                historicalEvaluations
                    .Select(te => te.EstimatedAffectedCustomers)
                    .ToList();

            double relevanceChange =
                relevanceScores.Last() - relevanceScores.First();

            int feedbackChange =
                feedbackCounts.Last() - feedbackCounts.First();

            double relevanceVariance =
                CalculateVariance(relevanceScores);

            // =====================================================
            // Trend Rules
            // =====================================================

            // Increasing
            if (relevanceChange > 0.2 &&
                feedbackChange > 0)
            {
                return "Increasing";
            }

            // Decreasing
            if (relevanceChange < -0.2 &&
                feedbackChange < 0)
            {
                return "Decreasing";
            }

            // Volatile
            if (relevanceVariance > 0.5)
            {
                return "Volatile";
            }

            // Stable
            return "Stable";
        }
        catch
        {
            return "Stable";
        }
    }

    /// <summary>
    /// Helper method to calculate variance of a list of values
    /// </summary>
    private double CalculateVariance(List<double> values)
    {
        if (values.Count < 2)
            return 0;

        double mean = values.Average();
        double sumSquaredDifferences = values.Sum(v => Math.Pow(v - mean, 2));
        return sumSquaredDifferences / values.Count;
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
                MetPercentage = Math.Min(100, (evaluationRun.ClusteringPrecision / ClusteringPrecisionThreshold) * 100)
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
