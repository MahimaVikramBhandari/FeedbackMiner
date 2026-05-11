using System.ComponentModel.DataAnnotations;

public class ProcessingRun
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the processing run
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Number of feedback items processed
    /// </summary>
    public int FeedbackItemCount { get; set; }

    /// <summary>
    /// Number of clusters created
    /// </summary>
    public int ClusterCount { get; set; }

    /// <summary>
    /// Number of themes extracted
    /// </summary>
    public int ThemeCount { get; set; }

    /// <summary>
    /// Average silhouette score for all clusters
    /// </summary>
    public double AverageClusterQuality { get; set; }

    /// <summary>
    /// Duplicate clustering precision score
    /// </summary>
    public double DuplicateDetectionPrecision { get; set; }

    /// <summary>
    /// Average theme relevance score
    /// </summary>
    public double AverageThemeRelevance { get; set; }

    /// <summary>
    /// Average action recommendation usefulness
    /// </summary>
    public double AverageActionUsefulness { get; set; }

    /// <summary>
    /// Embedding model used
    /// </summary>
    public string EmbeddingModel { get; set; }

    /// <summary>
    /// Clustering algorithm used
    /// </summary>
    public string ClusteringAlgorithm { get; set; }

    /// <summary>
    /// Parameters used for clustering
    /// </summary>
    public string ParametersJson { get; set; }

    /// <summary>
    /// Processing start time
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Processing completion time
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Status: Pending, Running, Completed, Failed
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    // Navigation
    public ICollection<ThemeCluster> Clusters { get; set; } = new List<ThemeCluster>();
}
