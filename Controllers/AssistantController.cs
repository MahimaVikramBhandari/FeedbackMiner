using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/assistant")]
public class AssistantController : ControllerBase
{
    private readonly OpenAIService _openAIService;

    public AssistantController(OpenAIService openAIService)
    {
        _openAIService = openAIService;
    }

    [HttpPost("dashboard-guide")]
    public async Task<IActionResult> AskDashboardGuide([FromBody] AssistantQuestionRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest(new { success = false, error = "Question is required." });
            }

            var answer = await _openAIService.AskDashboardAssistantAsync(request.Question.Trim());

            return Ok(new
            {
                success = true,
                data = new AssistantAnswerResponse
                {
                    Answer = answer
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}

public class AssistantQuestionRequest
{
    public string Question { get; set; } = string.Empty;
}

public class AssistantAnswerResponse
{
    public string Answer { get; set; } = string.Empty;
}
