public class FeedbackItem
{
    public Guid Id { get; set; }

    public string Source { get; set; }
    public string Text { get; set; }
    public string ProcessedText { get; set; }

    public int? Rating { get; set; }

    public string ProductArea { get; set; }
    public string Category { get; set; }
    public string CustomerSegment { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Language { get; set; }

    public string MetadataJson { get; set; }

    // 👇 PREP FOR NEXT STEP
    public string EmbeddingJson { get; set; } // store vector as JSON (temporary approach)
}