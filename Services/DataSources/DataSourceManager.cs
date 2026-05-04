using Microsoft.Extensions.Logging;

/// <summary>
/// Service for managing feedback source adapters and importing data
/// </summary>
public class DataSourceManager
{
    private readonly Dictionary<string, IFeedbackSourceAdapter> _adapters;
    private readonly FeedbackDbContext _dbContext;
    private readonly ILogger<DataSourceManager> _logger;

    public DataSourceManager(FeedbackDbContext dbContext, ILogger<DataSourceManager> logger)
    {
        _dbContext = dbContext;
        _logger = logger;

        _adapters = new Dictionary<string, IFeedbackSourceAdapter>(StringComparer.OrdinalIgnoreCase)
        {
            { "CSV", new CsvFeedbackAdapter() },
            { "JSON", new JsonFeedbackAdapter() }
        };
    }

    /// <summary>
    /// Get available adapters
    /// </summary>
    public List<string> GetAvailableAdapters() => _adapters.Keys.ToList();

    /// <summary>
    /// Import feedback from a data source
    /// </summary>
    public async Task<ImportResult> ImportFeedbackAsync(
        string sourceType,
        Dictionary<string, string> credentials,
        DateTime? since = null)
    {
        var result = new ImportResult
        {
            SourceType = sourceType,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            if (!_adapters.TryGetValue(sourceType, out var adapter))
            {
                result.Success = false;
                result.ErrorMessage = $"Unknown source type: {sourceType}";
                return result;
            }

            _logger.LogInformation($"Testing connection to {adapter.GetSourceName()}");
            if (!await adapter.TestConnectionAsync(credentials))
            {
                result.Success = false;
                result.ErrorMessage = "Connection test failed";
                return result;
            }

            _logger.LogInformation($"Fetching feedback from {adapter.GetSourceName()}");
            var feedbackItems = await adapter.FetchFeedbackAsync(credentials, since);

            if (feedbackItems.Count == 0)
            {
                result.Success = true;
                result.ImportedCount = 0;
                result.Message = "No new feedback items found";
                return result;
            }

            _logger.LogInformation($"Saving {feedbackItems.Count} feedback items to database");

            // Add to database
            _dbContext.FeedbackItems.AddRange(feedbackItems);
            await _dbContext.SaveChangesAsync();

            result.Success = true;
            result.ImportedCount = feedbackItems.Count;
            result.Message = $"Successfully imported {feedbackItems.Count} feedback items";

            _logger.LogInformation(result.Message);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error importing feedback: {ex.Message}");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Get required credentials for a source type
    /// </summary>
    public List<string> GetRequiredCredentials(string sourceType)
    {
        if (!_adapters.TryGetValue(sourceType, out var adapter))
            return new List<string>();

        return adapter.GetRequiredCredentials();
    }

    /// <summary>
    /// Register a custom adapter
    /// </summary>
    public void RegisterAdapter(string name, IFeedbackSourceAdapter adapter)
    {
        _adapters[name] = adapter;
        _logger.LogInformation($"Registered adapter: {name}");
    }
}

/// <summary>
/// Result of a feedback import operation
/// </summary>
public class ImportResult
{
    public string SourceType { get; set; }
    public bool Success { get; set; }
    public int ImportedCount { get; set; }
    public string Message { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
