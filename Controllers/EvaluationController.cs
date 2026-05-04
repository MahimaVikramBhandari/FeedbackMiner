using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/evaluation")]
public class EvaluationController : ControllerBase
{
    private readonly EvaluationMetricsService _evaluationService;
    private readonly FeedbackDbContext _dbContext;

    public EvaluationController(
        EvaluationMetricsService evaluationService,
        FeedbackDbContext dbContext)
    {
        _evaluationService = evaluationService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Evaluate a processing run against quality metrics
    /// </summary>
    [HttpPost("evaluate/{processingRunId}")]
    public async Task<IActionResult> EvaluateRun(Guid processingRunId)
    {
        try
        {
            var evaluationRun = await _evaluationService.EvaluateProcessingRunAsync(processingRunId);
            var summary = _evaluationService.GetEvaluationSummary(evaluationRun);

            return Ok(new
            {
                success = true,
                message = "Evaluation completed successfully",
                data = summary
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get evaluation metrics for a processing run
    /// </summary>
    [HttpGet("metrics/{processingRunId}")]
    public IActionResult GetEvaluationMetrics(Guid processingRunId)
    {
        try
        {
            var evaluationRun = _dbContext.EvaluationRuns
                .FirstOrDefault(er => er.ProcessingRunId == processingRunId);

            if (evaluationRun == null)
                return NotFound(new
                {
                    success = false,
                    error = "No evaluation found for this processing run"
                });

            var summary = _evaluationService.GetEvaluationSummary(evaluationRun);

            return Ok(new
            {
                success = true,
                data = summary
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get theme evaluations for a processing run
    /// </summary>
    [HttpGet("themes/{processingRunId}")]
    public IActionResult GetThemeEvaluations(Guid processingRunId)
    {
        try
        {
            var evaluationRun = _dbContext.EvaluationRuns
                .FirstOrDefault(er => er.ProcessingRunId == processingRunId);

            if (evaluationRun == null)
                return NotFound(new
                {
                    success = false,
                    error = "No evaluation found for this processing run"
                });

            var themeEvaluations = _dbContext.ThemeEvaluations
                .Where(te => te.EvaluationRunId == evaluationRun.Id)
                .Include(te => te.Theme)
                .Select(te => new
                {
                    ThemeId = te.Theme.Id,
                    ThemeLabel = te.Theme.Label,
                    RelevanceScore = te.RelevanceScore,
                    MetThreshold = te.MetRelevanceThreshold,
                    FeedbackCount = te.EstimatedAffectedCustomers,
                    FeedbackPercentage = te.FeedbackPercentage,
                    Trend = te.Trend,
                    ReviewStatus = te.ReviewStatus
                })
                .ToList();

            return Ok(new
            {
                success = true,
                count = themeEvaluations.Count,
                data = themeEvaluations
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get action recommendation evaluations for a processing run
    /// </summary>
    [HttpGet("recommendations/{processingRunId}")]
    public IActionResult GetRecommendationEvaluations(Guid processingRunId)
    {
        try
        {
            var evaluationRun = _dbContext.EvaluationRuns
                .FirstOrDefault(er => er.ProcessingRunId == processingRunId);

            if (evaluationRun == null)
                return NotFound(new
                {
                    success = false,
                    error = "No evaluation found for this processing run"
                });

            var recommendationEvaluations = _dbContext.ActionRecommendationEvaluations
                .Where(are => are.EvaluationRunId == evaluationRun.Id)
                .Include(are => are.ActionRecommendation)
                .Select(are => new
                {
                    RecommendationId = are.ActionRecommendation.Id,
                    Title = are.ActionRecommendation.Title,
                    UsefulnessScore = are.UsefulnessScore,
                    MetThreshold = are.MetUsefulnessThreshold,
                    FeasibilityScore = are.FeasibilityScore,
                    Priority = are.ActionRecommendation.Priority,
                    Status = are.Status,
                    ReviewNotes = are.ReviewNotes
                })
                .ToList();

            return Ok(new
            {
                success = true,
                count = recommendationEvaluations.Count,
                data = recommendationEvaluations
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get all evaluations with optional filtering
    /// </summary>
    [HttpGet("history")]
    public IActionResult GetEvaluationHistory([FromQuery] int pageSize = 10, [FromQuery] int page = 0)
    {
        try
        {
            var evaluations = _dbContext.EvaluationRuns
                .OrderByDescending(er => er.CreatedAt)
                .Skip(page * pageSize)
                .Take(pageSize)
                .Select(er => new
                {
                    EvaluationRunId = er.Id,
                    ProcessingRunId = er.ProcessingRunId,
                    CreatedAt = er.CreatedAt,
                    CompletedAt = er.CompletedAt,
                    Status = er.Status,
                    ThemeRelevance = new
                    {
                        Score = er.AverageThemeRelevanceScore,
                        MetPercentage = er.ThemeRelevanceMetPercentage
                    },
                    ClusteringPrecision = er.ClusteringPrecision,
                    RecommendationUsefulness = new
                    {
                        Score = er.AverageRecommendationUsefulnessScore,
                        MetPercentage = er.RecommendationUsefulnessMetPercentage
                    },
                    OverallQualityScore = er.OverallQualityScore
                })
                .ToList();

            return Ok(new
            {
                success = true,
                count = evaluations.Count,
                page,
                data = evaluations
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }
}
