using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

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
            {
                throw new ArgumentException("FilePath credential is required");
            }

            var filePath = credentials["FilePath"];
            
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            bool exists = File.Exists(filePath);
            return exists;
        }
        catch (Exception ex)
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
                // Configure CsvHelper to allow missing fields (properties not in CSV)
                csv.Context.RegisterClassMap<CsvFeedbackRecordMap>();
                
                var records = csv.GetRecords<CsvFeedbackRecord>().ToList();

                foreach (var record in records)
                {
                    var createdDate = ParseDate(record.CreatedAt);
                    if (since.HasValue && createdDate < since.Value)
                        continue;

                    var item = new FeedbackItem
                    {
                        Id = Guid.NewGuid(),
                        Source = !string.IsNullOrWhiteSpace(record.Source) ? record.Source : "CSV Import",
                        Text = !string.IsNullOrWhiteSpace(record.Text) ? record.Text : (!string.IsNullOrWhiteSpace(record.Feedback) ? record.Feedback : ""),
                        ProcessedText = !string.IsNullOrWhiteSpace(record.Text) ? record.Text : (!string.IsNullOrWhiteSpace(record.Feedback) ? record.Feedback : ""),
                        Language = "en",
                        CreatedOn = createdDate
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

/// <summary>
/// CsvHelper mapping for CsvFeedbackRecord to allow optional fields
/// </summary>
public class CsvFeedbackRecordMap : CsvHelper.Configuration.ClassMap<CsvFeedbackRecord>
{
    public CsvFeedbackRecordMap()
    {
        Map(m => m.Text).Optional();
        Map(m => m.Feedback).Optional();
        Map(m => m.Rating).Optional();
        Map(m => m.ProductArea).Optional();
        Map(m => m.Category).Optional();
        Map(m => m.CustomerSegment).Optional();
        Map(m => m.CreatedAt).Optional();
        Map(m => m.Source).Optional();
    }
}
