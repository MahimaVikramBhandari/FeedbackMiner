using System.ComponentModel.DataAnnotations;

public class Theme
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the processing run that created this theme
    /// </summary>
    public Guid? ProcessingRunId { get; set; }

    /// <summary>
    /// Descriptive theme label (e.g., "Performance Issues", "Poor Customer Support")
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Detailed description of the theme
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Theme relevance score (1-5)
    /// </summary>
    public double RelevanceScore { get; set; }

    /// <summary>
    /// Number of feedback items tagged with this theme
    /// </summary>
    public int FeedbackCount { get; set; }

    /// <summary>
    /// Average sentiment of feedback items in this theme
    /// </summary>
    public double AverageSentimentScore { get; set; }

    /// <summary>
    /// Average urgency level of feedback items
    /// </summary>
    public double AverageUrgencyScore { get; set; }

    /// <summary>
    /// Impact score based on customer segments and issue severity
    /// </summary>
    public double ImpactScore { get; set; }

    /// <summary>
    /// Product areas affected by this theme
    /// </summary>
    public string AffectedProductAreasJson { get; set; } // Store as JSON array

    /// <summary>
    /// Customer segments most affected
    /// </summary>
    public string AffectedSegmentsJson { get; set; } // Store as JSON array

    /// <summary>
    /// When the theme was first identified
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update to the theme
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Hash for tracking theme stability across runs
    /// </summary>
    public string ThemeHashCode { get; set; }

    // Navigation
    public ICollection<FeedbackItem> FeedbackItems { get; set; } = new List<FeedbackItem>();
    public ICollection<ThemeCluster> Clusters { get; set; } = new List<ThemeCluster>();
    public ICollection<ActionRecommendation> ActionRecommendations { get; set; } = new List<ActionRecommendation>();
}
