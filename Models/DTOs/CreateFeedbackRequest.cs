
public class CreateFeedbackRequest
{
    public string Source { get; set; }
    public string Text { get; set; }
    public int? Rating { get; set; }

    public string ProductArea { get; set; }
    public string Category { get; set; }
    public string CustomerSegment { get; set; }

    public Dictionary<string, string> Metadata { get; set; }
}