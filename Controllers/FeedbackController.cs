using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

[ApiController]
[Route("api/feedback")]
public class FeedbackController : ControllerBase
{
    private readonly FeedbackService _service;

    public FeedbackController(FeedbackService service)
    {
        _service = service;
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
    public async Task<IActionResult> Create([FromBody] CreateFeedbackRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Request body is null"
                });
            }

            var item = new FeedbackItem
            {
                Id = Guid.NewGuid(),
                Source = request.Source,
                Text = request.Text,
                Rating = request.Rating,
                ProductArea = request.ProductArea,
                Category = request.Category,
                CustomerSegment = request.CustomerSegment,
                CreatedAt = DateTime.UtcNow,
                MetadataJson = JsonConvert.SerializeObject(request.Metadata)
            };

            await _service.IngestAsync(item);

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
}