/// <summary>
/// Service for clustering similar feedback items using embeddings
/// Implements K-means like clustering with duplicate detection
/// </summary>
public class ClusteringService
{
    private readonly EmbeddingService _embeddingService;
    private const double SimilarityThreshold = 0.75; // Threshold for duplicate detection
    private const double DefaultMinSimilarity = 0.5; // Minimum similarity to join cluster

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

            cluster.SilhouetteScore = CalculateSilhouetteScore(cluster, clusters, embeddings);

            clusters.Add(cluster);

            // Check if max clusters reached
            if (maxClusters.HasValue && clusters.Count >= maxClusters.Value)
                break;
        }

        return clusters;
    }

    /// <summary>
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
        if (cluster.Items.Count == 1)
            return 1.0; // Single-item clusters have perfect silhouette

        double totalSilhouette = 0;

        foreach (var item in cluster.Items)
        {
            if (!embeddings.ContainsKey(item.Id))
                continue;

            // Within-cluster distance (a)
            var a = cluster.Items
                .Where(i => i.Id != item.Id && embeddings.ContainsKey(i.Id))
                .Average(i => 1 - _embeddingService.CalculateSimilarity(
                    embeddings[item.Id],
                    embeddings[i.Id]));

            // Between-cluster distance (b)
            var b = double.MaxValue;
            foreach (var otherCluster in allClusters.Where(c => c.ClusterNumber != cluster.ClusterNumber))
            {
                var avgDistance = otherCluster.Items
                    .Where(i => embeddings.ContainsKey(i.Id))
                    .Average(i => 1 - _embeddingService.CalculateSimilarity(
                        embeddings[item.Id],
                        embeddings[i.Id]));

                b = Math.Min(b, avgDistance);
            }

            if (b == double.MaxValue)
                b = 0;

            var s = (b - a) / Math.Max(a, b);
            totalSilhouette += s;
        }

        return totalSilhouette / cluster.Items.Count;
    }
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
