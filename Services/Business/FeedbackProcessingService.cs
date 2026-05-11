using System.Text.Json;
using System.Text.RegularExpressions;
using static ClusteringService;

/// <summary>
/// Orchestrates the entire feedback processing pipeline:
/// Ingestion -> Embedding -> Clustering -> Theme Labeling -> Sentiment Analysis -> Action Recommendations
/// </summary>
public class FeedbackProcessingService
{
    private readonly IFeedbackRepository _repository;
    private readonly TextProcessingPipeline _textPipeline;
    private readonly EmbeddingService _embeddingService;
    private readonly ClusteringService _clusteringService;
    private readonly SentimentAnalysisService _sentimentService;
    private readonly ThemeLabelingService _themeLabelingService;
    private readonly ActionRecommendationService _actionService;
    private readonly FeedbackDbContext _dbContext;

    public FeedbackProcessingService(
        IFeedbackRepository repository,
        TextProcessingPipeline textPipeline,
        EmbeddingService embeddingService,
        ClusteringService clusteringService,
        SentimentAnalysisService sentimentService,
        ThemeLabelingService themeLabelingService,
        ActionRecommendationService actionService,
        FeedbackDbContext dbContext)
    {
        _repository = repository;
        _textPipeline = textPipeline;
        _embeddingService = embeddingService;
        _clusteringService = clusteringService;
        _sentimentService = sentimentService;
        _themeLabelingService = themeLabelingService;
        _actionService = actionService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Run complete feedback analysis pipeline
    /// </summary>
    public async Task<ProcessingRun> RunFullPipelineAsync(
        List<FeedbackItem> feedbackItems,
        string runName = null,
        double clusterSimilarityThreshold = 0.50)
    {
        if (feedbackItems.Count == 0)
            throw new ArgumentException("No feedback items to process", nameof(feedbackItems));

        var runId = Guid.NewGuid();
        var processingRun = new ProcessingRun
        {
            Id = runId,
            Name = runName ?? $"Run-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            FeedbackItemCount = feedbackItems.Count,
            EmbeddingModel = "text-embedding-3-small",
            ClusteringAlgorithm = "Similarity-based K-means",
            ParametersJson = JsonSerializer.Serialize(new { clusterSimilarityThreshold }),
            StartedAt = DateTime.UtcNow,
            Status = "Running"
        };

        // Persist the run immediately so dependent entities (ThemeClusters) can reference it.
        _dbContext.ProcessingRuns.Add(processingRun);
        await _dbContext.SaveChangesAsync();

        try
        {
            // Step 1: Generate embeddings for all feedback
            Console.WriteLine("Step 1: Generating embeddings...");
            await GenerateEmbeddingsAsync(feedbackItems);

            // Step 2: Cluster similar feedback items
            Console.WriteLine("Step 2: Clustering feedback items...");
            var clusters = await _clusteringService.ClusterByEmbeddingAsync(
                feedbackItems,
                clusterSimilarityThreshold);

            processingRun.ClusterCount = clusters.Count;

            // Step 3: Analyze sentiment and urgency for each item
            Console.WriteLine("Step 3: Analyzing sentiment and urgency...");
            await AnalyzeSentimentAsync(feedbackItems);

            // Step 4: Create theme clusters in database
            Console.WriteLine("Step 4: Creating theme clusters...");
            var dbClusters = await CreateThemeClustersAsync(clusters, processingRun);

            // Step 5: Label themes and create Theme entities
            Console.WriteLine("Step 5: Labeling themes...");
            var themes = await LabelThemesAsync(dbClusters, feedbackItems, processingRun.Id);
            processingRun.ThemeCount = themes.Count;

            // Step 6: Generate action recommendations
            Console.WriteLine("Step 6: Generating action recommendations...");
            await GenerateActionRecommendationsAsync(themes, feedbackItems);

            // Step 7: Calculate metrics
            Console.WriteLine("Step 7: Calculating metrics...");
            CalculateMetrics(processingRun, clusters, themes);

            processingRun.Status = "Completed";
            processingRun.CompletedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return processingRun;
        }
        catch (Exception ex)
        {
            processingRun.Status = "Failed";
            processingRun.ErrorMessage = ex.Message;
            processingRun.CompletedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            throw;
        }
    }

    private async Task GenerateEmbeddingsAsync(List<FeedbackItem> items)
    {

        var textsToEmbed = items
            //.Where(i => string.IsNullOrEmpty(i.EmbeddingJson))
            .Select(i =>
            {
                var text = i.ProcessedText ?? i.Text;

                // Remove PII placeholder tokens before embedding generation
                // to improve semantic similarity and clustering quality
                text = text
                    .Replace("REDACTED_EMAIL", "")
                    .Replace("REDACTED_PHONE", "")
                    .Replace("REDACTED_NAME", "")
                    .Replace("REDACTED_CARD", "")
                    .Replace("REDACTED_IP", "");

                // Normalize extra spaces after replacements
                text = Regex.Replace(text, @"\s{2,}", " ").Trim();

                return text;
            })
            .ToList();

        if (textsToEmbed.Count == 0)
            return;

        var embeddings = await _embeddingService.GenerateEmbeddingsBatchAsync(textsToEmbed);

        foreach (var item in items)
            //.Where(i => string.IsNullOrEmpty(i.EmbeddingJson)))
        {
            var text = item.ProcessedText ?? item.Text;
            text = text
                    .Replace("REDACTED_EMAIL", "")
                    .Replace("REDACTED_PHONE", "")
                    .Replace("REDACTED_NAME", "")
                    .Replace("REDACTED_CARD", "")
                    .Replace("REDACTED_IP", "");
            text = Regex.Replace(text, @"\s{2,}", " ").Trim();

            if (embeddings.ContainsKey(text))
            {
                item.EmbeddingJson = _embeddingService.SerializeEmbedding(embeddings[text]);
            }
        }

        await _repository.SaveChangesAsync();
    }

    private async Task AnalyzeSentimentAsync(List<FeedbackItem> items)
    {
        var itemsNeedingAnalysis = items
            .Where(i => i.SentimentScore == null)
            .ToList();

        if (itemsNeedingAnalysis.Count == 0)
            return;

        var texts = itemsNeedingAnalysis.Select(i => i.ProcessedText ?? i.Text).ToList();
        var results = await _sentimentService.AnalyzeBatchAsync(texts);

        for (int i = 0; i < itemsNeedingAnalysis.Count && i < results.Count; i++)
        {
            itemsNeedingAnalysis[i].SentimentScore = results[i].SentimentScore;
            itemsNeedingAnalysis[i].SentimentLabel = results[i].SentimentLabel;
            itemsNeedingAnalysis[i].UrgencyScore = results[i].UrgencyScore;
            itemsNeedingAnalysis[i].UrgencyLevel = results[i].UrgencyLevel;
        }

        await _repository.SaveChangesAsync();
    }

    private async Task<List<ThemeCluster>> CreateThemeClustersAsync(
        List<FeedbackCluster> clusters,
        ProcessingRun processingRun)
    {
        var dbClusters = new List<ThemeCluster>();

        foreach (var cluster in clusters)
        {
            var dbCluster = new ThemeCluster
            {
                Id = Guid.NewGuid(),
                ProcessingRunId = processingRun.Id,
                ClusterNumber = cluster.ClusterNumber,
                ItemCount = cluster.Items.Count,
                AverageSimilarity = cluster.AverageSimilarity,
                SilhouetteScore = cluster.SilhouetteScore,
                CentroidEmbeddingJson = _embeddingService.SerializeEmbedding(cluster.CentroidEmbedding),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.ThemeClusters.Add(dbCluster);

            // Associate feedback items with cluster
            foreach (var item in cluster.Items)
            {
                item.ThemeClusterId = dbCluster.Id;
            }

            dbClusters.Add(dbCluster);
        }

        await _dbContext.SaveChangesAsync();
        return dbClusters;
    }

    private async Task<List<Theme>> LabelThemesAsync(
        List<ThemeCluster> clusters,
        List<FeedbackItem> allItems, Guid processingRunId)
    {
        var themes = new List<Theme>();

        foreach (var cluster in clusters)
        {
            var clusterItems = allItems.Where(i => i.ThemeClusterId == cluster.Id).ToList();
            if (clusterItems.Count == 0)
                continue;

            // Generate theme label
            var labelResult = await _themeLabelingService.LabelClusterAsync(clusterItems);

            // Create theme
            var theme = new Theme
            {
                Id = Guid.NewGuid(),
                ProcessingRunId = processingRunId,
                Label = labelResult.Label,
                Description = labelResult.Description,
                RelevanceScore = labelResult.RelevanceScore,
                FeedbackCount = clusterItems.Count,
                AverageSentimentScore = clusterItems.Average(i => i.SentimentScore ?? 0),
                AverageUrgencyScore = clusterItems.Average(i => i.UrgencyScore ?? 0),
                AffectedProductAreasJson = JsonSerializer.Serialize(labelResult.AffectedAreas),
                AffectedSegmentsJson = JsonSerializer.Serialize(labelResult.AffectedSegments),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ThemeHashCode = GenerateThemeHash(labelResult.Label, labelResult.AffectedAreas)
            };

            // Calculate impact score
            theme.ImpactScore = CalculateImpactScore(theme, clusterItems);

            _dbContext.Themes.Add(theme);
            cluster.SuggestedThemeId = theme.Id;

            // Associate items with theme
            foreach (var item in clusterItems)
            {
                item.ThemeId = theme.Id;
            }

            themes.Add(theme);
        }

        await _dbContext.SaveChangesAsync();
        return themes;
    }

    private async Task GenerateActionRecommendationsAsync(
        List<Theme> themes,
        List<FeedbackItem> allItems)
    {
        foreach (var theme in themes)
        {
            var relatedItems = allItems.Where(i => i.ThemeId == theme.Id).ToList();
            if (relatedItems.Count == 0)
                continue;

            // Generate recommendations
            var recommendations = await _actionService.GenerateRecommendationsAsync(
                theme,
                relatedItems);

            // Save recommendations
            foreach (var rec in recommendations)
            {
                var actionRec = new ActionRecommendation
                {
                    Id = Guid.NewGuid(),
                    ThemeId = theme.Id,
                    Title = rec.Title,
                    Description = rec.Description,
                    Category = rec.Category,
                    Priority = rec.Priority,
                    EstimatedEffort = rec.EstimatedEffort,
                    ImpactScore = rec.ImpactScore,
                    AffectedAreasJson = rec.AffectedAreas,
                    BenefitSegmentsJson = rec.BenefitSegments,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Evaluate usefulness
                actionRec.UsefulnessRating = await _actionService.EvaluateUsefulnessAsync(actionRec, relatedItems);

                _dbContext.ActionRecommendations.Add(actionRec);
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    private void CalculateMetrics(
            ProcessingRun run,
            List<FeedbackCluster> clusters,
            List<Theme> themes)
    {
        // -----------------------------
        // Average Cluster Quality
        // -----------------------------
        if (clusters.Count > 0)
        {
            run.AverageClusterQuality = Math.Round(
                clusters.Average(c => c.SilhouetteScore),
                3);
        }

        // -----------------------------
        // Average Theme Relevance
        // -----------------------------
        if (themes.Count > 0)
        {
            run.AverageThemeRelevance = Math.Round(
                themes.Average(t => t.RelevanceScore),
                3);
        }

        // -----------------------------
        // Duplicate Detection Precision
        // -----------------------------
        if (clusters.Count > 0)
        {
            // Ignore single-item clusters because they do not
            // provide meaningful duplicate grouping evaluation.
            var validClusters = clusters
                .Where(c => c.Items.Count > 1)
                .ToList();

            if (validClusters.Count > 0)
            {
                // Average similarity measures semantic cohesion.
                var avgSimilarity = validClusters
                    .Average(c => c.AverageSimilarity);

                // Silhouette measures cluster separation quality.
                var avgSilhouette = validClusters
                    .Average(c => Math.Max(c.SilhouetteScore, 0));

                // Cluster density bonus rewards larger cohesive clusters.
                var densityBonus = Math.Min(
                    validClusters.Average(c => c.Items.Count) / 5.0,
                    0.15);

                // Final weighted precision formula
                run.DuplicateDetectionPrecision = Math.Round(
                    Math.Min(
                        1.0,
                        (avgSimilarity * 0.5) +
                        (avgSilhouette * 0.35) +
                        densityBonus),
                    3);
            }
            else
            {
                run.DuplicateDetectionPrecision = 0;
            }
        }

        // -----------------------------
        // Average Action Usefulness
        // -----------------------------
        var allRecommendations = _dbContext.ActionRecommendations
            .Where(a => a.UsefulnessRating != null)
            .ToList();

        if (allRecommendations.Count > 0)
        {
            // Scale usefulness to 1-5 range
            run.AverageActionUsefulness = Math.Round(
                allRecommendations.Average(a => a.UsefulnessRating ?? 0),
                3);
        }
        else
        {
            run.AverageActionUsefulness = 0;
        }
    }

    private double CalculateImpactScore(Theme theme, List<FeedbackItem> items)
    {
        // Calculate impact based on:
        // - Number of feedback items (spread)
        // - Average urgency level (severity)
        // - Average sentiment negativity (user dissatisfaction)

        var countScore = Math.Min(items.Count / 10.0, 1.0); // 0-1
        var urgencyScore = items.Average(i => i.UrgencyScore ?? 0); // 0-1
        var sentimentScore = -Math.Min(items.Average(i => i.SentimentScore ?? 0), 0); // 0-1 (invert to negative)

        var impact = (countScore * 0.3) + (urgencyScore * 0.4) + (sentimentScore * 0.3);
        return Math.Clamp(impact * 5, 1, 5); // Scale to 1-5
    }

    private string GenerateThemeHash(string label, List<string> affectedAreas)
    {
        var combined = $"{label}|{string.Join(",", affectedAreas)}";
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
            return Convert.ToBase64String(hashedBytes).Substring(0, 16);
        }
    }
}
