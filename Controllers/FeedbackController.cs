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

    [HttpPost]
    public async Task<IActionResult> Create(CreateFeedbackRequest request)
    {
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

        return Ok();
    }
}