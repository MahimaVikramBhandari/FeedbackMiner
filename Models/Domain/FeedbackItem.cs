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

    public string? Language { get; set; }

    public string? MetadataJson { get; set; }

    public string? EmbeddingJson { get; set; }

    // Sentiment and urgency analysis
    public double? SentimentScore { get; set; } // -1 to 1
    public string? SentimentLabel { get; set; } // Positive, Negative, Neutral
    public double? UrgencyScore { get; set; } // 0 to 1
    public string? UrgencyLevel { get; set; } // Low, Medium, High, Critical

    // Clustering
    public Guid? ThemeClusterId { get; set; }
    public ThemeCluster ThemeCluster { get; set; }

    // Theme tracking
    public Guid? ThemeId { get; set; }
    public Theme Theme { get; set; }

    // Similarity to other items (for duplicate detection)
    public double? SimilarityScore { get; set; }
}