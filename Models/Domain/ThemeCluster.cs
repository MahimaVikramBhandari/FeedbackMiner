using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ThemeCluster
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Unique cluster identifier for a run
    /// </summary>
    public int ClusterNumber { get; set; }

    /// <summary>
    /// The processing run this cluster belongs to
    /// </summary>
    public Guid ProcessingRunId { get; set; }

    /// <summary>
    /// Center point of the cluster (representative embedding)
    /// </summary>
    public string CentroidEmbeddingJson { get; set; }

    /// <summary>
    /// Number of items in this cluster
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Average similarity within cluster
    /// </summary>
    public double AverageSimilarity { get; set; }

    /// <summary>
    /// Silhouette score for cluster quality (0-1)
    /// </summary>
    public double SilhouetteScore { get; set; }

    /// <summary>
    /// Suggested theme for this cluster
    /// </summary>
    public Guid? SuggestedThemeId { get; set; }

    [ForeignKey(nameof(SuggestedThemeId))]
    public Theme SuggestedTheme { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation
    public ICollection<FeedbackItem> FeedbackItems { get; set; } = new List<FeedbackItem>();
}
