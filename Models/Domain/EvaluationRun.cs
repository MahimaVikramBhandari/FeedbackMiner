using System.ComponentModel.DataAnnotations;

public class EvaluationRun
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the processing run being evaluated
    /// </summary>
    public Guid ProcessingRunId { get; set; }
    public ProcessingRun ProcessingRun { get; set; }

    /// <summary>
    /// Average theme relevance score (target >= 4.0/5.0)
    /// </summary>
    public double AverageThemeRelevanceScore { get; set; }

    /// <summary>
    /// Percentage of themes meeting relevance >= 4.0 threshold
    /// </summary>
    public double ThemeRelevanceMetPercentage { get; set; }

    /// <summary>
    /// Clustering precision (target >= 0.8)
    /// Measured as: correctly clustered duplicates / total clustered items
    /// </summary>
    public double ClusteringPrecision { get; set; }

    /// <summary>
    /// Duplicate detection rate
    /// Measured as: items correctly identified as duplicates / total duplicates
    /// </summary>
    public double DuplicateDetectionRate { get; set; }

    /// <summary>
    /// Average action recommendation usefulness score (target >= 4.0/5.0)
    /// </summary>
    public double AverageRecommendationUsefulnessScore { get; set; }

    /// <summary>
    /// Percentage of recommendations meeting usefulness >= 4.0 threshold
    /// </summary>
    public double RecommendationUsefulnessMetPercentage { get; set; }

    /// <summary>
    /// Average silhouette score for clusters (higher is better, -1 to 1)
    /// </summary>
    public double AverageSilhouetteScore { get; set; }

    /// <summary>
    /// Overall pipeline quality score (weighted average of all metrics)
    /// </summary>
    public double OverallQualityScore { get; set; }

    /// <summary>
    /// Evaluation status
    /// </summary>
    public string Status { get; set; } // "InProgress", "Completed", "Failed"

    /// <summary>
    /// Any errors encountered during evaluation
    /// </summary>
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Detailed evaluation notes
    /// </summary>
    public string NotesJson { get; set; }

    // Navigation properties
    public ICollection<ThemeEvaluation> ThemeEvaluations { get; set; } = new List<ThemeEvaluation>();
    public ICollection<ActionRecommendationEvaluation> ActionRecommendationEvaluations { get; set; } = new List<ActionRecommendationEvaluation>();
}
