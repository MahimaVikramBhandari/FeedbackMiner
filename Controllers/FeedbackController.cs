using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

[ApiController]
[Route("api/feedback")]
public class FeedbackController : ControllerBase
{
    private readonly FeedbackService _feedbackService;

    public FeedbackController(FeedbackService service)
    {
        _feedbackService = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent([FromQuery] int take = 100)
    {
        try
        {
            var items = await _service.GetRecentAsync(Math.Clamp(take, 1, 500));

            var result = items.Select(item => new
            {
                item.Id,
                item.Source,
                item.Text,
                item.ProcessedText,
                item.Rating,
                item.ProductArea,
                item.Category,
                item.CustomerSegment,
                item.CreatedAt,
                item.Language,
                item.MetadataJson,
                item.SentimentScore,
                item.SentimentLabel,
                item.UrgencyScore,
                item.UrgencyLevel,
                item.ThemeClusterId,
                item.ThemeId,
                item.SimilarityScore
            }).ToList();

            return Ok(new
            {
                success = true,
                count = result.Count,
                data = result
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "Feedback data could not be loaded.",
                details = ex.InnerException?.Message ?? ex.Message
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateFeedbackRequest request)
    {
        var item = new FeedbackItem
        {
            Id = Guid.NewGuid(),
            Source = request.Source,
            Text = request.Text
        };

        await _feedbackService.IngestAsync(item);

            return Ok(new
            {
                success = true,
                data = new
                {
                    item.Id,
                    item.Source,
                    item.Text,
                    item.ProductArea,
                    item.Category,
                    item.CustomerSegment,
                    item.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = "Feedback could not be saved.",
                details = ex.InnerException?.Message ?? ex.Message
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<FeedbackItem>> GetFeedback(Guid Id) 
    {
        var result = await _feedbackService.GetFeedbackById(Id);
        if (result == null)
        {
            return NotFound(new
            {
                success = false,
                error = $"Feedback with id {Id} not found"
            });
        }

        return Ok(result);
    }
}