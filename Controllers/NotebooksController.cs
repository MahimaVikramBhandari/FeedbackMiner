using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/notebooks")]
public class NotebooksController : ControllerBase
{
    private readonly EvaluationNotebookService _notebookService;

    public NotebooksController(EvaluationNotebookService notebookService)
    {
        _notebookService = notebookService;
    }

    /// <summary>
    /// Generate evaluation notebook for a processing run
    /// </summary>
    [HttpPost("generate/{processingRunId}")]
    public async Task<IActionResult> GenerateNotebook(Guid processingRunId)
    {
        try
        {
            var notebook = await _notebookService.GenerateNotebookAsync(processingRunId);

            return Ok(new
            {
                success = true,
                message = "Notebook generated successfully",
                data = notebook
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
    /// Export notebook as JSON
    /// </summary>
    [HttpGet("export/json/{processingRunId}")]
    public async Task<IActionResult> ExportAsJson(Guid processingRunId)
    {
        try
        {
            var notebook = await _notebookService.GenerateNotebookAsync(processingRunId);
            var json = _notebookService.ExportAsJson(notebook);

            return File(
                System.Text.Encoding.UTF8.GetBytes(json),
                "application/json",
                $"evaluation-{processingRunId:N}.json"
            );
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
    /// Export notebook as HTML
    /// </summary>
    [HttpGet("export/html/{processingRunId}")]
    public async Task<IActionResult> ExportAsHtml(Guid processingRunId)
    {
        try
        {
            var notebook = await _notebookService.GenerateNotebookAsync(processingRunId);
            var html = _notebookService.ExportAsHtml(notebook);

            return File(
                System.Text.Encoding.UTF8.GetBytes(html),
                "text/html",
                $"evaluation-{processingRunId:N}.html"
            );
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
