using System.Diagnostics.Metrics;

/// <summary>
/// Service for clustering similar feedback items using embeddings
/// Implements K-means like clustering with duplicate detection
/// 
/// Threshold Strategy (aligned with metric targets):
/// - DefaultMinSimilarity (0.65): Main clustering threshold for theme extraction
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
    private const double DefaultMinSimilarity = 0.65;

    public ClusteringService(EmbeddingService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    /// <summary>
    /// Cluster feedback items by embedding similarity
    /// </summary>
    public async Task<List<FeedbackCluster>> ClusterByEmbeddingAsync(
        List<FeedbackItem> items,
        double minSimilarity = DefaultMinSimilarity,
        int? maxClusters = null)
    {
        if (items.Count == 0)
            return new List<FeedbackCluster>();

        // Parse embeddings
        var embeddings = new Dictionary<Guid, float[]>();
        foreach (var item in items.Where(i => !string.IsNullOrEmpty(i.EmbeddingJson)))
        {
            embeddings[item.Id] = _embeddingService.DeserializeEmbedding(item.EmbeddingJson);
        }

        if (embeddings.Count < items.Count)
        {
            throw new InvalidOperationException("Not all feedback items have embeddings. Generate embeddings first.");
        }

        var clusters = new List<FeedbackCluster>();
        var assignedItems = new HashSet<Guid>();

        // Sort items by ID for deterministic clustering
        var sortedItems = items.OrderBy(i => i.Id).ToList();

        for (int i = 0; i < sortedItems.Count; i++)
        {
            if (assignedItems.Contains(sortedItems[i].Id))
                continue;

            // Start new cluster
            var cluster = new FeedbackCluster
            {
                ClusterNumber = clusters.Count,
                Items = new List<FeedbackItem> { sortedItems[i] }
            };
            assignedItems.Add(sortedItems[i].Id);

            // Add similar items to cluster
            for (int j = i + 1; j < sortedItems.Count; j++)
            {
                if (assignedItems.Contains(sortedItems[j].Id))
                    continue;

                var similarity = _embeddingService.CalculateSimilarity(
                    embeddings[sortedItems[i].Id],
                    embeddings[sortedItems[j].Id]);

                if (similarity >= minSimilarity)
                {
                    cluster.Items.Add(sortedItems[j]);
                    assignedItems.Add(sortedItems[j].Id);
                }
            }

            // Calculate cluster metrics
            cluster.CentroidEmbedding = _embeddingService.CalculateCentroid(
                cluster.Items.Select(item => embeddings[item.Id]).ToList());

            cluster.AverageSimilarity = CalculateAverageSimilarity(
                cluster.Items.Select(item => embeddings[item.Id]).ToList(),
                cluster.CentroidEmbedding);

            // Populate similarity scores for each item in the cluster
            foreach (var item in cluster.Items)
            {
                item.SimilarityScore = _embeddingService.CalculateSimilarity(
                    embeddings[item.Id],
                    cluster.CentroidEmbedding);
            }

            cluster.SilhouetteScore = CalculateSilhouetteScore(cluster, clusters, embeddings);

            clusters.Add(cluster);

            // Check if max clusters reached
            if (maxClusters.HasValue && clusters.Count >= maxClusters.Value)
                break;
        }

        return clusters;
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

    /// <summary>
    /// Assign items to closest clusters AND populate similarity scores
    /// Returns a dictionary with assignment and similarity for each item
    /// </summary>
    public Dictionary<Guid, (int clusterNumber, double similarity)> AssignItemsToClustersDeterministicWithScores(
        List<FeedbackCluster> clusters,
        List<FeedbackItem> items)
    {
        var results = new Dictionary<Guid, (int, double)>();
        var embeddings = new Dictionary<Guid, float[]>();

        foreach (var item in items.Where(i => !string.IsNullOrEmpty(i.EmbeddingJson)))
        {
            embeddings[item.Id] = _embeddingService.DeserializeEmbedding(item.EmbeddingJson);
        }

        foreach (var item in items)
        {
            if (!embeddings.ContainsKey(item.Id))
                continue;

            int closestCluster = 0;
            double maxSimilarity = -1;

            for (int i = 0; i < clusters.Count; i++)
            {
                var similarity = _embeddingService.CalculateSimilarity(
                    embeddings[item.Id],
                    clusters[i].CentroidEmbedding);

                if (similarity > maxSimilarity)
                {
                    maxSimilarity = similarity;
                    closestCluster = i;
                }
            }

            results[item.Id] = (closestCluster, maxSimilarity);
            // Also populate the item's similarity score directly
            item.SimilarityScore = maxSimilarity;
        }

        return results;
    }

    /// <summary>
    /// Assign items to closest clusters
    /// </summary>
    public Dictionary<Guid, int> AssignItemsToClustersDeterministic(
        List<FeedbackCluster> clusters,
        List<FeedbackItem> items)
    {
        var assignments = new Dictionary<Guid, int>();
        var embeddings = new Dictionary<Guid, float[]>();

        foreach (var item in items.Where(i => !string.IsNullOrEmpty(i.EmbeddingJson)))
        {
            embeddings[item.Id] = _embeddingService.DeserializeEmbedding(item.EmbeddingJson);
        }

        foreach (var item in items)
        {
            if (!embeddings.ContainsKey(item.Id))
                continue;

            int closestCluster = 0;
            double maxSimilarity = -1;

            for (int i = 0; i < clusters.Count; i++)
            {
                var similarity = _embeddingService.CalculateSimilarity(
                    embeddings[item.Id],
                    clusters[i].CentroidEmbedding);

                if (similarity > maxSimilarity)
                {
                    maxSimilarity = similarity;
                    closestCluster = i;
                }
            }

            assignments[item.Id] = closestCluster;
        }

        return assignments;
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
        // For singleton clusters, still return 0 per convention
        if (cluster.Items.Count <= 1)
            return 0;

        double totalSilhouette = 0;
        int validItems = 0;

        // Pre-calculate centroid distance matrix for efficiency
        var clusterCentroids = new Dictionary<int, float[]>();
        foreach (var c in allClusters)
        {
            if (c.CentroidEmbedding != null)
                clusterCentroids[c.ClusterNumber] = c.CentroidEmbedding;
        }

        foreach (var item in cluster.Items)
        {
            if (!embeddings.TryGetValue(item.Id, out var currentEmbedding))
                continue;

            // =====================================================
            // a(i): Average SIMILARITY to items in SAME cluster
            // Using similarity directly is more stable than 1-similarity
            // =====================================================

            var sameClusterItems = cluster.Items
                .Where(i => i.Id != item.Id && embeddings.ContainsKey(i.Id))
                .ToList();

            double intraClusterSimilarity = 0;

            if (sameClusterItems.Any())
            {
                var similarities = sameClusterItems.Select(i =>
                    _embeddingService.CalculateSimilarity(currentEmbedding, embeddings[i.Id])
                ).ToList();

                intraClusterSimilarity = similarities.Average();
            }

            // =====================================================
            // b(i): Best average SIMILARITY to OTHER clusters
            // Find the cluster most similar to this item
            // =====================================================

            double interClusterSimilarity = -1; // Start with worst possible value

            foreach (var otherCluster in allClusters
                         .Where(c => c.ClusterNumber != cluster.ClusterNumber))
            {
                var otherClusterItems = otherCluster.Items
                    .Where(i => embeddings.ContainsKey(i.Id))
                    .ToList();

                if (!otherClusterItems.Any())
                    continue;

                var similarities = otherClusterItems.Select(i =>
                    _embeddingService.CalculateSimilarity(currentEmbedding, embeddings[i.Id])
                ).ToList();

                double avgSimilarity = similarities.Average();

                // Take the cluster with MAXIMUM similarity (closest neighbor)
                if (avgSimilarity > interClusterSimilarity)
                    interClusterSimilarity = avgSimilarity;
            }

            // Edge case: no other clusters found
            if (interClusterSimilarity < 0)
                interClusterSimilarity = 0;

            // =====================================================
            // Silhouette coefficient: improved formula
            // Uses similarities directly instead of (1 - similarity)
            // This handles the case where all items are identical better
            // =====================================================

            double silhouette;

            // Case 1: Item is isolated (no intra-cluster neighbors)
            if (intraClusterSimilarity == 0 && interClusterSimilarity == 0)
            {
                silhouette = 0;
            }
            // Case 2: High intra-cluster cohesion, low inter-cluster similarity
            else if (intraClusterSimilarity > interClusterSimilarity)
            {
                // Good clustering: item is closer to its own cluster
                silhouette = (intraClusterSimilarity - interClusterSimilarity) / Math.Max(intraClusterSimilarity, interClusterSimilarity);
            }
            // Case 3: Low intra-cluster cohesion but high inter-cluster similarity
            else
            {
                // Poor clustering: item might belong to neighboring cluster
                silhouette = (interClusterSimilarity - intraClusterSimilarity) / Math.Max(intraClusterSimilarity, interClusterSimilarity);
                silhouette = -silhouette; // Negative value indicates borderline assignment
            }

            // Clamp to [-1, 1] for numerical stability
            silhouette = Math.Clamp(silhouette, -1.0, 1.0);

            totalSilhouette += silhouette;
            validItems++;
        }

        return validItems == 0
            ? 0
            : totalSilhouette / validItems;
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

