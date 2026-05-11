using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json; 

[ApiController]
[Route("api/feedback")]
public class FeedbackController : ControllerBase
{
    private readonly FeedbackService _feedbackService;
    private readonly FeedbackDbContext _dbContext;

    public FeedbackController(FeedbackService service, FeedbackDbContext dbContext)
    {
        _feedbackService = service;
        _dbContext = dbContext;
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
    public IActionResult GetFeedbackList([FromQuery] int take = 100)
    {
        var items = _dbContext.FeedbackItems
            .OrderByDescending(f => f.CreatedOn)
            .Take(Math.Clamp(take, 1, 500))
            .Select(f => new FeedbackSummaryDto
            {
                Id = f.Id,
                Text = f.Text,
                ProcessedText = f.ProcessedText ?? f.Text,
                Source = f.Source,
                Language = f.Language,
                SentimentScore = f.SentimentScore,
                SentimentLabel = f.SentimentLabel,
                UrgencyScore = f.UrgencyScore,
                UrgencyLevel = f.UrgencyLevel,
                CreatedAt = f.CreatedOn
            })
            .ToList();

        return Ok(new
        {
            success = true,
            count = items.Count,
            data = items
        });
    }

    [HttpGet("{id:guid}")]
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
