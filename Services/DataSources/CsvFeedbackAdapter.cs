using System.Globalization;
using CsvHelper;

/// <summary>
/// CSV file adapter for importing feedback from CSV files
/// </summary>
public class CsvFeedbackAdapter : IFeedbackSourceAdapter
{
    public string GetSourceName() => "CSV File";

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
                throw new FileNotFoundException($"CSV file not found: {filePath}");

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<CsvFeedbackRecord>().ToList();

                foreach (var record in records)
                {
                    var createdDate = ParseDate(record.CreatedAt);
                    if (since.HasValue && createdDate < since.Value)
                        continue;

                    var item = new FeedbackItem
                    {
                        Id = Guid.NewGuid(),
                        Source = "CSV Import",
                        Text = record.Text ?? record.Feedback ?? "",
                        ProcessedText = record.Text ?? record.Feedback ?? "",
                        Rating = ParseInt(record.Rating),
                        ProductArea = record.ProductArea ?? "General",
                        Category = record.Category ?? "Feedback",
                        CustomerSegment = record.CustomerSegment ?? "Unknown",
                        Language = "en",
                        CreatedAt = createdDate,
                        MetadataJson = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            original_fields = record
                        })
                    };

                    feedbackItems.Add(item);
                }
            }

            return await Task.FromResult(feedbackItems);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error reading CSV file: {ex.Message}", ex);
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

    private int? ParseInt(string intString)
    {
        if (string.IsNullOrEmpty(intString))
            return null;

        if (int.TryParse(intString, out var value))
            return value;

        return null;
    }
}

/// <summary>
/// CSV record format
/// </summary>
public class CsvFeedbackRecord
{
    public string Text { get; set; }
    public string Feedback { get; set; }
    public string Rating { get; set; }
    public string ProductArea { get; set; }
    public string Category { get; set; }
    public string CustomerSegment { get; set; }
    public string CreatedAt { get; set; }
    public string Source { get; set; }
}
