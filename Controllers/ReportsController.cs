using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ReportingService _reportingService;

    public ReportsController(ReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    /// <summary>
    /// Get weekly digest of feedback activity
    /// </summary>
    [HttpGet("weekly-digest")]
    public async Task<IActionResult> GetWeeklyDigest([FromQuery] DateTime? weekStart = null)
    {
        try
        {
            var digest = await _reportingService.GetWeeklyDigestAsync(weekStart);
            return Ok(new { success = true, data = digest });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Export clusters from a processing run
    /// </summary>
    [HttpGet("clusters-export/{processingRunId}")]
    public async Task<IActionResult> ExportClusters(Guid processingRunId)
    {
        try
        {
            var clusters = await _reportingService.ExportClustersAsync(processingRunId);

            if (clusters.Count == 0)
                return NotFound(new { success = false, error = "No clusters found for this run" });

            return Ok(new { success = true, count = clusters.Count, data = clusters });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get processing run metrics
    /// </summary>
    [HttpGet("run-metrics/{runId}")]
    public async Task<IActionResult> GetRunMetrics(Guid runId)
    {
        try
        {
            var metrics = await _reportingService.GetProcessingRunMetricsAsync(runId);

            if (metrics == null)
                return NotFound(new { success = false, error = "Processing run not found" });

            return Ok(new { success = true, data = metrics });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get all processing runs
    /// </summary>
    [HttpGet("runs")]
    public async Task<IActionResult> GetAllRuns()
    {
        try
        {
            var runs = await _reportingService.GetAllProcessingRunsAsync();
            return Ok(new { success = true, count = runs.Count, data = runs });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Download weekly digest as CSV
    /// </summary>
    [HttpGet("weekly-digest-csv")]
    public async Task<IActionResult> ExportWeeklyDigestCsv([FromQuery] DateTime? weekStart = null)
    {
        try
        {
            var digest = await _reportingService.GetWeeklyDigestAsync(weekStart);
            var csv = GenerateCsv(digest);

            return File(System.Text.Encoding.UTF8.GetBytes(csv), 
                "text/csv", 
                $"weekly-digest-{DateTime.UtcNow:yyyyMMdd}.csv");
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    private string GenerateCsv(WeeklyDigestDto digest)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("WEEKLY FEEDBACK DIGEST");
        sb.AppendLine($"Week of,{digest.WeekStart:yyyy-MM-dd} to {digest.WeekEnd:yyyy-MM-dd}");
        sb.AppendLine();

        sb.AppendLine("SUMMARY METRICS");
        sb.AppendLine($"Total Feedback Received,{digest.TotalFeedbackReceived}");
        sb.AppendLine($"New Themes Identified,{digest.NewThemesIdentified}");
        sb.AppendLine($"Active Themes,{digest.ActiveThemes}");
        sb.AppendLine($"Average Sentiment Score,{digest.AverageSentiment:F2}");
        sb.AppendLine($"Critical Urgency Items,{digest.CriticalUrgencyCount}");
        sb.AppendLine();

        sb.AppendLine("TOP THEMES BY IMPACT");
        sb.AppendLine("Theme Label,Impact Score,Feedback Count,Relevance Score,Average Urgency");
        foreach (var theme in digest.TopThemesByImpact)
        {
            sb.AppendLine($"\"{theme.Label}\",{theme.ImpactScore:F2},{theme.FeedbackCount},{theme.RelevanceScore:F2},{theme.AverageUrgencyScore:F2}");
        }
        sb.AppendLine();

        sb.AppendLine("HIGH PRIORITY ACTIONS");
        sb.AppendLine("Action Title,Priority,Category,Impact Score,Usefulness Rating");
        foreach (var action in digest.HighPriorityActions)
        {
            sb.AppendLine($"\"{action.Title}\",{action.Priority},{action.Category},{action.ImpactScore:F2},{action.UsefulnessRating?.ToString("F2") ?? "N/A"}");
        }
        sb.AppendLine();

        sb.AppendLine("SOURCE BREAKDOWN");
        foreach (var source in digest.FeedbackSourceBreakdown)
        {
            sb.AppendLine($"{source.Key},{source.Value}");
        }
        sb.AppendLine();

        sb.AppendLine("SENTIMENT BREAKDOWN");
        foreach (var sentiment in digest.SentimentBreakdown)
        {
            sb.AppendLine($"{sentiment.Key},{sentiment.Value}");
        }

        return sb.ToString();
    }
}
