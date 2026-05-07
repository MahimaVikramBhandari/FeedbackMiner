using System.ComponentModel.DataAnnotations;

public class ThemeEvaluation
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the theme being evaluated
    /// </summary>
    public Guid ThemeId { get; set; }
    public Theme Theme { get; set; }

    /// <summary>
    /// Reference to the evaluation run
    /// </summary>
    public Guid EvaluationRunId { get; set; }
    public EvaluationRun EvaluationRun { get; set; }

    /// <summary>
    /// Theme relevance score (1-5 scale)
    /// </summary>
    public double RelevanceScore { get; set; }

    /// <summary>
    /// Whether this theme met the relevance >= 4.0 threshold
    /// </summary>
    public bool MetRelevanceThreshold { get; set; }

    /// <summary>
    /// Estimated unique customers affected by this theme
    /// </summary>
    public int EstimatedAffectedCustomers { get; set; }

    /// <summary>
    /// Percentage of total feedback in this theme
    /// </summary>
    public double FeedbackPercentage { get; set; }

    /// <summary>
    /// Trend: is this theme increasing, stable, or decreasing
    /// </summary>
    public string Trend { get; set; } // "Increasing", "Stable", "Decreasing"

    public DateTime CreatedAt { get; set; }
}
