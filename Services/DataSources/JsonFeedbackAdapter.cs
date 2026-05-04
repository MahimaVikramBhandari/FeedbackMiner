using System.Text.Json;

/// <summary>
/// JSON file adapter for importing feedback from JSON files
/// </summary>
public class JsonFeedbackAdapter : IFeedbackSourceAdapter
{
    public string GetSourceName() => "JSON File";

    public List<string> GetRequiredCredentials() => new() { "FilePath" };

    public async Task<bool> TestConnectionAsync(Dictionary<string, string> credentials)
    {
        try
        {
            if (!credentials.ContainsKey("FilePath"))
                return false;

            var filePath = credentials["FilePath"];
            return File.Exists(filePath);
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<FeedbackItem>> FetchFeedbackAsync(Dictionary<string, string> credentials, DateTime? since = null)
    {
        var feedbackItems = new List<FeedbackItem>();

        try
        {
            if (!credentials.ContainsKey("FilePath"))
                throw new ArgumentException("FilePath credential is required");

            var filePath = credentials["FilePath"];

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"JSON file not found: {filePath}");

            var json = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var records = JsonSerializer.Deserialize<List<JsonFeedbackRecord>>(json, options);

            if (records == null)
                throw new InvalidOperationException("Invalid JSON format");

            foreach (var record in records)
            {
                var createdDate = ParseDate(record.CreatedAt);
                if (since.HasValue && createdDate < since.Value)
                    continue;

                var item = new FeedbackItem
                {
                    Id = Guid.NewGuid(),
                    Source = record.Source ?? "JSON Import",
                    Text = record.Text ?? record.Feedback ?? "",
                    ProcessedText = record.Text ?? record.Feedback ?? "",
                    Rating = record.Rating,
                    ProductArea = record.ProductArea ?? "General",
                    Category = record.Category ?? "Feedback",
                    CustomerSegment = record.CustomerSegment ?? "Unknown",
                    Language = record.Language ?? "en",
                    CreatedAt = createdDate,
                    MetadataJson = JsonSerializer.Serialize(record)
                };

                feedbackItems.Add(item);
            }

            return feedbackItems;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error reading JSON file: {ex.Message}", ex);
        }
    }

    private DateTime ParseDate(string dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return DateTime.UtcNow;

        if (DateTime.TryParse(dateString, out var date))
            return date;

        return DateTime.UtcNow;
    }
}

/// <summary>
/// JSON feedback record format
/// </summary>
public class JsonFeedbackRecord
{
    public string Text { get; set; }
    public string Feedback { get; set; }
    public int? Rating { get; set; }
    public string ProductArea { get; set; }
    public string Category { get; set; }
    public string CustomerSegment { get; set; }
    public string Language { get; set; }
    public string CreatedAt { get; set; }
    public string Source { get; set; }
}
