using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/analysis")]
public class AnalysisController : ControllerBase
{
    private readonly FeedbackProcessingService _processingService;
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly FeedbackDbContext _dbContext;

    public AnalysisController(
        FeedbackProcessingService processingService,
        IFeedbackRepository feedbackRepository,
        FeedbackDbContext dbContext)
    {
        _processingService = processingService;
        _feedbackRepository = feedbackRepository;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Trigger full feedback analysis pipeline
    /// Uses optimized clustering threshold to meet quality metrics:
    /// - Theme Relevance >= 4.0/5.0
    /// - Clustering Precision >= 0.8
    /// - Recommendation Usefulness >= 4.0/5.0
    /// </summary>
    [HttpPost("run-pipeline")]
    public async Task<IActionResult> RunPipeline([FromBody] RunPipelineRequest request)
    {
        try
        {
            // Get all unprocessed feedback or all feedback if specified
            var feedbackItems = request.ProcessAllFeedback
                ? _dbContext.FeedbackItems.ToList()
                : _dbContext.FeedbackItems
                    .Where(f => f.ThemeClusterId == null)
                    .ToList();

            if (feedbackItems.Count == 0)
                return BadRequest(new { success = false, error = "No feedback items to process" });

            // Use fixed optimal clustering threshold from centralized configuration
            var clusteringThreshold = ClusteringConfiguration.ClusteringThreshold;

            var processingRun = await _processingService.RunFullPipelineAsync(
                feedbackItems,
                request.RunName ?? $"Auto-Run-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
                clusteringThreshold);

            return Ok(new
            {
                success = true,
                message = "Pipeline executed successfully",
                data = new
                {
                    runId = processingRun.Id,
                    runName = processingRun.Name,
                    feedbackProcessed = processingRun.FeedbackItemCount,
                    clustersCreated = processingRun.ClusterCount,
                    themesExtracted = processingRun.ThemeCount,
                    status = processingRun.Status,
                    completedAt = processingRun.CompletedAt,
                    clusteringThreshold = clusteringThreshold,
                    clusteringThresholdLabel = ClusteringConfiguration.GetThresholdLabel(clusteringThreshold)
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    /// <summary>
    /// Find duplicate feedback items using consistent high-similarity threshold
    /// </summary>
    [HttpGet("duplicates")]
    public IActionResult FindDuplicates([FromQuery] int limit = 50)
    {
        try
        {
            var embeddingService = new EmbeddingService();
            var clusteringService = new ClusteringService(embeddingService);

            var feedbackItems = _dbContext.FeedbackItems
                .Where(f => !string.IsNullOrEmpty(f.EmbeddingJson))
                .ToList();

            // Use high similarity threshold from centralized configuration
            var duplicateThreshold = ClusteringConfiguration.DuplicateDetectionThreshold;

            var duplicates = clusteringService.FindDuplicates(feedbackItems, duplicateThreshold)
                .Take(limit)
                .Select(d => new
                {
                    item1 = new { d.Item1.Id, text = d.Item1.ProcessedText },
                    item2 = new { d.Item2.Id, text = d.Item2.ProcessedText },
                    similarity = d.Item3,
                    isDuplicate = d.Item3 >= duplicateThreshold
                })
                .ToList();

            return Ok(new
            {
                success = true,
                count = duplicates.Count,
                threshold = duplicateThreshold,
                thresholdLabel = ClusteringConfiguration.GetThresholdLabel(duplicateThreshold),
                message = $"Duplicates detected using high-similarity threshold ({duplicateThreshold:F2})",
                data = duplicates
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Re-run sentiment analysis
    /// </summary>
    [HttpPost("rescan-sentiment")]
    public async Task<IActionResult> RescanSentiment([FromBody] RescanRequest request)
    {
        try
        {
            var sentimentService = new SentimentAnalysisService();
            var query = _dbContext.FeedbackItems.AsQueryable();

            if (request.ThemeId.HasValue)
                query = query.Where(f => f.ThemeId == request.ThemeId);

            var items = query.ToList();

            if (items.Count == 0)
                return BadRequest(new { success = false, error = "No items found for sentiment analysis" });

            var texts = items.Select(i => i.ProcessedText ?? i.Text).ToList();
            var results = await sentimentService.AnalyzeBatchAsync(texts);

            for (int i = 0; i < items.Count && i < results.Count; i++)
            {
                items[i].SentimentScore = results[i].SentimentScore;
                items[i].SentimentLabel = results[i].SentimentLabel;
                items[i].UrgencyScore = results[i].UrgencyScore;
                items[i].UrgencyLevel = results[i].UrgencyLevel;
            }

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Rescanned sentiment for {items.Count} items",
                itemsProcessed = items.Count
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get clustering configuration and metric targets
    /// Helps users understand the system's quality targets and thresholds
    /// </summary>
    [HttpGet("configuration")]
    public IActionResult GetConfiguration()
    {
        return Ok(new
        {
            success = true,
            message = "Clustering and evaluation configuration",
            clustering = new
            {
                primaryThreshold = new
                {
                    value = ClusteringConfiguration.ClusteringThreshold,
                    description = "Threshold for main feedback clustering",
                    rationale = "Balances tight clustering (precision) with relevance preservation",
                    label = ClusteringConfiguration.GetThresholdLabel(ClusteringConfiguration.ClusteringThreshold)
                },
                duplicateDetectionThreshold = new
                {
                    value = ClusteringConfiguration.DuplicateDetectionThreshold,
                    description = "Threshold for strict near-duplicate detection",
                    rationale = "Only matches items with very high semantic similarity"
                },
                silhouetteQualityThreshold = new
                {
                    value = ClusteringConfiguration.SilhouetteQualityThreshold,
                    description = "Silhouette score threshold for well-formed clusters",
                    rationale = "Clusters above this score trigger quality bonuses"
                }
            },
            qualityMetricTargets = new
            {
                themeRelevance = new
                {
                    target = ClusteringConfiguration.MetricTargets.ThemeRelevanceTarget,
                    scale = "1-5",
                    description = "Theme must be relevant to the feedback"
                },
                clusteringPrecision = new
                {
                    target = ClusteringConfiguration.MetricTargets.ClusteringPrecisionTarget,
                    scale = "0-1",
                    description = "Clustering quality - separation and cohesion"
                },
                recommendationUsefulness = new
                {
                    target = ClusteringConfiguration.MetricTargets.RecommendationUsefulnessTarget,
                    scale = "1-5",
                    description = "Action recommendation must be useful for addressing feedback"
                }
            },
            qualityMultipliers = new
            {
                usefulness = new
                {
                    clusterSize = new
                    {
                        large10Plus = $"{ClusteringConfiguration.UsefulnessMultipliers.ClusterSize.Large10Plus:P0}",
                        medium5Plus = $"{ClusteringConfiguration.UsefulnessMultipliers.ClusterSize.Medium5Plus:P0}",
                        small3Plus = $"{ClusteringConfiguration.UsefulnessMultipliers.ClusterSize.Small3Plus:P0}"
                    },
                    clusterQuality = $"{ClusteringConfiguration.UsefulnessMultipliers.HighQualityCluster:P0}",
                    impact = new
                    {
                        high4Plus = $"{ClusteringConfiguration.UsefulnessMultipliers.Impact.HighImpact4Plus:P0}",
                        medium3Plus = $"{ClusteringConfiguration.UsefulnessMultipliers.Impact.MediumImpact3Plus:P0}"
                    }
                },
                relevance = new
                {
                    highCohesion = $"{ClusteringConfiguration.RelevanceBonuses.HighCohesionBonus:P0}",
                    partialCohesion = $"{ClusteringConfiguration.RelevanceBonuses.PartialCohesionBonus:P0}"
                }
            }
        });
    }

    /// <summary>
    /// Check pipeline status
    /// </summary>
    [HttpGet("pipeline-status/{runId}")]
    public IActionResult GetPipelineStatus(Guid runId)
    {
        try
        {
            var run = _dbContext.ProcessingRuns.FirstOrDefault(pr => pr.Id == runId);

            if (run == null)
                return NotFound(new { success = false, error = "Processing run not found" });

            var duration = run.CompletedAt.HasValue
                ? (run.CompletedAt.Value - run.StartedAt).TotalSeconds
                : (DateTime.UtcNow - run.StartedAt).TotalSeconds;

            return Ok(new
            {
                success = true,
                data = new
                {
                    runId = run.Id,
                    name = run.Name,
                    status = run.Status,
                    feedbackProcessed = run.FeedbackItemCount,
                    clustersCreated = run.ClusterCount,
                    themesExtracted = run.ThemeCount,
                    averageClusterQuality = run.AverageClusterQuality,
                    duplicateDetectionPrecision = run.DuplicateDetectionPrecision,
                    averageThemeRelevance = run.AverageThemeRelevance,
                    averageActionUsefulness = run.AverageActionUsefulness,
                    durationSeconds = duration,
                    startedAt = run.StartedAt,
                    completedAt = run.CompletedAt,
                    errorMessage = run.ErrorMessage
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}

public class RunPipelineRequest
{
    public string RunName { get; set; }
    public bool ProcessAllFeedback { get; set; } = false;
}

public class RescanRequest
{
    public Guid? ThemeId { get; set; }
}
