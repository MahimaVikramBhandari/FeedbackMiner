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
            data = item
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetFeedback(Guid id)
    {
        var result = await _feedbackService.GetFeedbackById(id);

        if (result == null)
        {
            return NotFound(new
            {
                success = false,
                error = $"Feedback with id {id} not found"
            });
        }

        return Ok(new
        {
            success = true,
            data = result
        });
    }
}