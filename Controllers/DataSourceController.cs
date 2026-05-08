using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/data-sources")]
public class DataSourceController : ControllerBase
{
    private readonly DataSourceManager _dataSourceManager;
    private readonly ILogger<DataSourceController> _logger;

    public DataSourceController(
        DataSourceManager dataSourceManager,
        ILogger<DataSourceController> logger)
    {
        _dataSourceManager = dataSourceManager;
        _logger = logger;
    }

    /// <summary>
    /// Get available data sources
    /// </summary>
    [HttpGet("available")]
    public IActionResult GetAvailableSources()
    {
        try
        {
            var sources = _dataSourceManager.GetAvailableAdapters();
            var sourceDetails = sources.Select(s => new
            {
                Type = s,
                RequiredCredentials = _dataSourceManager.GetRequiredCredentials(s)
            }).ToList();

            return Ok(new
            {
                success = true,
                count = sourceDetails.Count,
                data = sourceDetails
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
    /// Import feedback from a data source
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> ImportFeedback([FromBody] ImportRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.SourceType))
                return BadRequest(new
                {
                    success = false,
                    error = "SourceType is required"
                });

            if (request.Credentials == null || request.Credentials.Count == 0)
                return BadRequest(new
                {
                    success = false,
                    error = "Credentials are required"
                });

            var result = await _dataSourceManager.ImportFeedbackAsync(
                request.SourceType,
                request.Credentials,
                request.Since);

            if (result.Success)
                return Ok(new
                {
                    success = true,
                    data = result
                });
            else
                return BadRequest(new
                {
                    success = false,
                    error = result.ErrorMessage,
                    data = result
                });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error importing feedback: {ex.Message}");
            return BadRequest(new
            {
                success = false,
                error = ex.Message
            });
        }
    }
}

/// <summary>
/// Request model for importing feedback
/// </summary>
public class ImportRequest
{
    public string SourceType { get; set; }
    public Dictionary<string, string> Credentials { get; set; }
    public DateTime? Since { get; set; }
}
