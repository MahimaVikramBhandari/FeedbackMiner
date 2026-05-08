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

            var processingRun = await _processingService.RunFullPipelineAsync(
                feedbackItems,
                request.RunName ?? $"Auto-Run-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
                request.ClusterSimilarityThreshold ?? 0.5);

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
                    completedAt = processingRun.CompletedAt
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
    /// Find duplicate feedback items
    /// </summary>
    [HttpGet("duplicates")]
    public IActionResult FindDuplicates([FromQuery] double threshold = 0.75, [FromQuery] int limit = 50)
    {
        try
        {
            var embeddingService = new EmbeddingService();
            var clusteringService = new ClusteringService(embeddingService);

            var feedbackItems = _dbContext.FeedbackItems
                .Where(f => !string.IsNullOrEmpty(f.EmbeddingJson))
                .ToList();

            var duplicates = clusteringService.FindDuplicates(feedbackItems, threshold)
                .Take(limit)
                .Select(d => new
                {
                    item1 = new { d.Item1.Id, text = d.Item1.ProcessedText },
                    item2 = new { d.Item2.Id, text = d.Item2.ProcessedText },
                    similarity = d.Item3
                })
                .ToList();

            return Ok(new
            {
                success = true,
                count = duplicates.Count,
                threshold = threshold,
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
    public double? ClusterSimilarityThreshold { get; set; }
}

public class RescanRequest
{
    public Guid? ThemeId { get; set; }
}
