using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/themes")]
public class ThemesController : ControllerBase
{
    private readonly ReportingService _reportingService;
    private readonly FeedbackDbContext _dbContext;

    public ThemesController(ReportingService reportingService, FeedbackDbContext dbContext)
    {
        _reportingService = reportingService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get theme dashboard with top themes
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] int pageSize = 10)
    {
        try
        {
            var dashboard = await _reportingService.GetThemeDashboardAsync(pageSize);
            return Ok(new { success = true, data = dashboard });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get theme details by ID
    /// </summary>
    [HttpGet("{themeId}")]
    public IActionResult GetTheme(Guid themeId)
    {
        try
        {
            var theme = _dbContext.Themes
                .FirstOrDefault(t => t.Id == themeId);

            if (theme == null)
                return NotFound(new { success = false, error = "Theme not found" });

            var relatedItems = _dbContext.FeedbackItems
                .Where(f => f.ThemeId == themeId)
                .Count();

            var recommendations = _dbContext.ActionRecommendations
                .Where(ar => ar.ThemeId == themeId)
                .Select(ar => new ActionRecommendationDto
                {
                    Id = ar.Id,
                    Title = ar.Title,
                    Description = ar.Description,
                    Category = ar.Category,
                    Priority = ar.Priority,
                    EstimatedEffort = ar.EstimatedEffort,
                    ImpactScore = ar.ImpactScore,
                    UsefulnessRating = ar.UsefulnessRating
                })
                .ToList();

            var dto = new ThemeDashboardDto
            {
                ThemeId = theme.Id,
                Label = theme.Label,
                Description = theme.Description,
                FeedbackCount = relatedItems,
                RelevanceScore = theme.RelevanceScore,
                ImpactScore = theme.ImpactScore,
                AverageSentimentScore = theme.AverageSentimentScore,
                AverageUrgencyScore = theme.AverageUrgencyScore,
                TopRecommendations = recommendations,
                CreatedAt = theme.CreatedAt,
                UpdatedAt = theme.UpdatedAt
            };

            return Ok(new { success = true, data = dto });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get all themes with optional filtering
    /// </summary>
    [HttpGet]
    public IActionResult GetThemes(
        [FromQuery] string? sortBy = "impact",
        [FromQuery] bool descending = true,
        [FromQuery] int take = 50)
    {
        try
        {
            var query = _dbContext.Themes.AsQueryable();

            // Sorting
            query = sortBy?.ToLower() switch
            {
                "relevance" => descending ? query.OrderByDescending(t => t.RelevanceScore) : query.OrderBy(t => t.RelevanceScore),
                "feedback" => descending ? query.OrderByDescending(t => t.FeedbackCount) : query.OrderBy(t => t.FeedbackCount),
                "urgency" => descending ? query.OrderByDescending(t => t.AverageUrgencyScore) : query.OrderBy(t => t.AverageUrgencyScore),
                _ => descending ? query.OrderByDescending(t => t.ImpactScore) : query.OrderBy(t => t.ImpactScore),
            };

            var themes = query.Take(take).ToList();

            var result = themes.Select(t => new
            {
                t.Id,
                t.Label,
                t.Description,
                t.RelevanceScore,
                t.ImpactScore,
                t.FeedbackCount,
                t.AverageSentimentScore,
                t.AverageUrgencyScore,
                t.CreatedAt,
                t.UpdatedAt
            }).ToList();

            return Ok(new { success = true, count = result.Count, data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get feedback items related to a theme
    /// </summary>
    [HttpGet("{themeId}/feedback")]
    public IActionResult GetThemeFeedback(Guid themeId, [FromQuery] int take = 20)
    {
        try
        {
            var feedbackItems = _dbContext.FeedbackItems
                .Where(f => f.ThemeId == themeId)
                .OrderByDescending(f => f.CreatedOn)
                .Take(take)
                .Select(f => new FeedbackSummaryDto
                {
                    Id = f.Id,
                    Text = f.Text,
                    ProcessedText = f.ProcessedText ?? f.Text,
                    Language = f.Language,
                    Source = f.Source,
                    SentimentScore = f.SentimentScore,
                    SentimentLabel = f.SentimentLabel,
                    UrgencyScore = f.UrgencyScore,
                    UrgencyLevel = f.UrgencyLevel,
                    CreatedAt = f.CreatedOn
                })
                .ToList();

            return Ok(new { success = true, count = feedbackItems.Count, data = feedbackItems });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get action recommendations for a theme
    /// </summary>
    [HttpGet("{themeId}/recommendations")]
    public IActionResult GetThemeRecommendations(Guid themeId)
    {
        try
        {
            var query = _dbContext.ActionRecommendations
                .Where(ar => ar.ThemeId == themeId);

            var recommendations = query
                .OrderByDescending(ar => ar.ImpactScore)
                .Select(ar => new ActionRecommendationDto
                {
                    Id = ar.Id,
                    Title = ar.Title,
                    Description = ar.Description,
                    Category = ar.Category,
                    Priority = ar.Priority,
                    EstimatedEffort = ar.EstimatedEffort,
                    ImpactScore = ar.ImpactScore,
                    UsefulnessRating = ar.UsefulnessRating
                })
                .ToList();

            return Ok(new { success = true, count = recommendations.Count, data = recommendations });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Update theme or take action
    /// </summary>
    [HttpPut("{themeId}")]
    public IActionResult UpdateTheme(Guid themeId, [FromBody] UpdateThemeRequest request)
    {
        try
        {
            var theme = _dbContext.Themes.FirstOrDefault(t => t.Id == themeId);
            if (theme == null)
                return NotFound(new { success = false, error = "Theme not found" });

            if (!string.IsNullOrEmpty(request.Label))
                theme.Label = request.Label;

            if (!string.IsNullOrEmpty(request.Description))
                theme.Description = request.Description;

            theme.UpdatedAt = DateTime.UtcNow;
            _dbContext.SaveChanges();

            return Ok(new { success = true, message = "Theme updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}

public class UpdateThemeRequest
{
    public string Label { get; set; }
    public string Description { get; set; }
}
