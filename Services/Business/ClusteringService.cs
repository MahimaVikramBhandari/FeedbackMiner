using System.Diagnostics;
using System.Diagnostics.Metrics;

/// <summary>
/// Service for clustering similar feedback items using embeddings
/// Implements K-means like clustering with duplicate detection
/// 
/// Threshold Strategy (aligned with metric targets):
/// - DefaultMinSimilarity (0.70): Main clustering threshold for theme extraction
///   Balances tight clustering (for precision >= 0.8) with relevance preservation
/// - SimilarityThreshold (0.85): Strict duplicate detection
///   Only flags near-identical items as duplicates
/// </summary>
public class ClusteringService
{
    private readonly EmbeddingService _embeddingService;

    // Threshold for near-duplicate detection (high similarity = identical content)
    private const double SimilarityThreshold = 0.85;

    // Main clustering threshold - balances precision and recall for quality metrics
    // Targets: Clustering Precision >= 0.8, Theme Relevance >= 4.0
    private const double DefaultMinSimilarity = 0.50;

    public ClusteringService(EmbeddingService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    
    public async Task<List<FeedbackCluster>> ClusterByEmbeddingAsync(
                    List<FeedbackItem> items,
                    double minSimilarity = DefaultMinSimilarity,
                    int? maxClusters = null)
    {
        if (items == null || items.Count == 0)
            return new List<FeedbackCluster>();

        // Parse embeddings
        var embeddings = new Dictionary<Guid, float[]>();

        foreach (var item in items.Where(i => !string.IsNullOrWhiteSpace(i.EmbeddingJson)))
        {
            embeddings[item.Id] =
                _embeddingService.DeserializeEmbedding(item.EmbeddingJson);
        }

        if (embeddings.Count < items.Count)
        {
            throw new InvalidOperationException(
                "Not all feedback items have embeddings. Generate embeddings first.");
        }

        var clusters = new List<FeedbackCluster>();
        var assignedItems = new HashSet<Guid>();

        // Deterministic ordering improves clustering consistency
        var sortedItems = items
            .OrderBy(i => i.Id)
            .ToList();

        for (int i = 0; i < sortedItems.Count; i++)
        {
            var seedItem = sortedItems[i];

            if (assignedItems.Contains(seedItem.Id))
                continue;

            // Create new cluster
            var cluster = new FeedbackCluster
            {
                ClusterNumber = clusters.Count,
                Items = new List<FeedbackItem> { seedItem }
            };

            assignedItems.Add(seedItem.Id);

            bool clusterExpanded;

            // Continue expanding cluster until no more matches found
            do
            {
                clusterExpanded = false;

                for (int j = 0; j < sortedItems.Count; j++)
                {
                    var candidate = sortedItems[j];

                    if (assignedItems.Contains(candidate.Id))
                        continue;

                    // Dynamically calculate current centroid
                    var centroid = _embeddingService.CalculateCentroid(
                        cluster.Items
                            .Select(item => embeddings[item.Id])
                            .ToList());

                    // Compare candidate against CURRENT cluster centroid
                    var similarity = _embeddingService.CalculateSimilarity(
                        centroid,
                        embeddings[candidate.Id]);

                    Debug.WriteLine(
                        $"Comparing '{seedItem.ProcessedText}' " +
                        $"WITH '{candidate.ProcessedText}' " +
                        $"| Similarity: {similarity}");



                    if (similarity >= minSimilarity)
                    {

                        Debug.WriteLine($"MATCH FOUND | Cluster: {cluster.ClusterNumber} | Similarity: {similarity}");

                        cluster.Items.Add(candidate);
                        assignedItems.Add(candidate.Id);

                        clusterExpanded = true;
                    }
                }

            } while (clusterExpanded);

            // Final centroid calculation
            cluster.CentroidEmbedding =
                _embeddingService.CalculateCentroid(
                    cluster.Items
                        .Select(item => embeddings[item.Id])
                        .ToList());

            // Average similarity within cluster
            cluster.AverageSimilarity =
                CalculateAverageSimilarity(
                    cluster.Items
                        .Select(item => embeddings[item.Id])
                        .ToList(),
                    cluster.CentroidEmbedding);

            // Populate similarity score for each item
            foreach (var item in cluster.Items)
            {
                item.SimilarityScore =
                    _embeddingService.CalculateSimilarity(
                        embeddings[item.Id],
                        cluster.CentroidEmbedding);
            }

            clusters.Add(cluster);

            // Respect max cluster limit if provided
            if (maxClusters.HasValue &&
                clusters.Count >= maxClusters.Value)
            {
                break;
            }
        }

        // Calculate silhouette scores AFTER all clusters exist
        foreach (var cluster in clusters)
        {
            cluster.SilhouetteScore =
                CalculateSilhouetteScore(
                    cluster,
                    clusters,
                    embeddings);
        }

        // Return largest clusters first
        return clusters
            .OrderByDescending(c => c.Items.Count)
            .ToList();
    }

    ///// <summary>
    /// Identify duplicate feedback items within clusters
    /// </summary>
    public List<(FeedbackItem, FeedbackItem, double)> FindDuplicates(
        List<FeedbackItem> items,
        double threshold = SimilarityThreshold)
    {
        var duplicates = new List<(FeedbackItem, FeedbackItem, double)>();

        // Parse embeddings
        var embeddings = new Dictionary<Guid, float[]>();
        foreach (var item in items.Where(i => !string.IsNullOrEmpty(i.EmbeddingJson)))
        {
            embeddings[item.Id] = _embeddingService.DeserializeEmbedding(item.EmbeddingJson);
        }

        for (int i = 0; i < items.Count; i++)
        {
            for (int j = i + 1; j < items.Count; j++)
            {
                if (!embeddings.ContainsKey(items[i].Id) || !embeddings.ContainsKey(items[j].Id))
                    continue;

                var similarity = _embeddingService.CalculateSimilarity(
                    embeddings[items[i].Id],
                    embeddings[items[j].Id]);

                if (similarity >= threshold)
                {
                    duplicates.Add((items[i], items[j], similarity));
                }
            }
        }

        return duplicates;
    }

    private double CalculateAverageSimilarity(List<float[]> embeddings, float[] centroid)
    {
        if (embeddings.Count == 0)
            return 0;

        var similarities = embeddings.Select(e => _embeddingService.CalculateSimilarity(e, centroid)).ToList();
        return similarities.Average();
    }

    
    private double CalculateSilhouetteScore(
        FeedbackCluster cluster,
        List<FeedbackCluster> allClusters,
        Dictionary<Guid, float[]> embeddings)
    {
        // Silhouette undefined for singleton clusters
        if (cluster.Items.Count <= 1)
            return 0;

        if (allClusters.Count <= 1)
            return 0;

        var scores = new List<double>();

        foreach (var item in cluster.Items)
        {
            if (!embeddings.TryGetValue(item.Id, out var currentEmbedding))
                continue;

            // -----------------------------------------
            // a(i): average intra-cluster DISTANCE
            // -----------------------------------------

            var sameClusterItems = cluster.Items
                .Where(i => i.Id != item.Id && embeddings.ContainsKey(i.Id))
                .ToList();

            double a = 0;

            if (sameClusterItems.Any())
            {
                a = sameClusterItems.Average(i =>
                {
                    var similarity =
                        _embeddingService.CalculateSimilarity(
                            currentEmbedding,
                            embeddings[i.Id]);

                    return 1 - similarity;
                });
            }

            // -----------------------------------------
            // b(i): minimum average distance to another cluster
            // -----------------------------------------

            double b = double.MaxValue;

            foreach (var otherCluster in allClusters
                         .Where(c => c.ClusterNumber != cluster.ClusterNumber))
            {
                var otherItems = otherCluster.Items
                    .Where(i => embeddings.ContainsKey(i.Id))
                    .ToList();

                if (!otherItems.Any())
                    continue;

                double avgDistance = otherItems.Average(i =>
                {
                    var similarity =
                        _embeddingService.CalculateSimilarity(
                            currentEmbedding,
                            embeddings[i.Id]);

                    return 1 - similarity;
                });

                b = Math.Min(b, avgDistance);
            }

            if (b == double.MaxValue)
                b = 0;

            // -----------------------------------------
            // Standard silhouette formula
            // -----------------------------------------

            double denominator = Math.Max(a, b);

            double silhouette =
                denominator == 0
                    ? 0
                    : (b - a) / denominator;

            silhouette = Math.Clamp(silhouette, -1.0, 1.0);

            scores.Add(silhouette);
        }

        return scores.Count == 0
            ? 0
            : scores.Average();
    }



    /// <summary>
    /// Represents a cluster of similar feedback items
    /// </summary>
    public class FeedbackCluster
    {
        public int ClusterNumber { get; set; }
        public List<FeedbackItem> Items { get; set; } = new List<FeedbackItem>();
        public float[] CentroidEmbedding { get; set; }
        public double AverageSimilarity { get; set; }
        public double SilhouetteScore { get; set; }
    }
}

