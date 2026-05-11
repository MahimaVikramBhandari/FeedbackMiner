using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for summarizing feedback analysis and metrics
/// </summary>
[ApiController]
[Route("api/summarize")]
public class SummarizeController : ControllerBase
{
    private readonly SummarizeService _summarizeService;
    private readonly ILogger<SummarizeController> _logger;

    public SummarizeController(
        SummarizeService summarizeService,
        ILogger<SummarizeController> logger)
    {
        _summarizeService = summarizeService;
        _logger = logger;
    }

    /// <summary>
    /// Handle any summarize question (generic endpoint)
    /// </summary>
    [HttpPost("ask")]
    public async Task<IActionResult> AskSummarizeQuestion([FromBody] SummarizeRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new { success = false, error = "Question is required." });

            var question = request.Question.Trim().ToLower();

            SummaryResponse result = question switch
            {
                var q when q.Contains("average theme relevance") =>
                    await _summarizeService.GetAverageThemeRelevanceAsync(),
                var q when q.Contains("cluster similarity") || q.Contains("cluster similarity score") =>
                    await _summarizeService.GetClusterSimilarityAverageAsync(),
                var q when q.Contains("feedback reports") =>
                    await _summarizeService.SummarizeFeedbackReportsAsync(),
                var q when q.Contains("weekly digest") =>
                    await _summarizeService.SummarizeWeeklyDigestAsync(),
                var q when q.Contains("cluster report") =>
                    await _summarizeService.SummarizeClusterReportAsync(),
                var q when q.Contains("evaluation notebook") =>
                    await _summarizeService.SummarizeEvaluationNotebookAsync(),
                var q when q.Contains("evaluation history") =>
                    await _summarizeService.SummarizeEvaluationHistoryAsync(),
                var q when q.Contains("all the feedbacks") || q.Contains("all feedbacks") =>
                    await _summarizeService.SummarizeAllFeedbacksAsync(),
                var q when q.Contains("generate notebook") =>
                    await _summarizeService.GenerateNotebookSummaryAsync(),

                // Fallback: treat as a free-form question related to FeedbackMiner
                _ => await _summarizeService.AnswerFeedbackMinerQuestionAsync(request.Question.Trim())
            };

            if (result.Success)
                return Ok(new { success = true, data = result });
            else
                return BadRequest(new { success = false, error = result.Summary });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing summarize question: {ex.Message}");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}
