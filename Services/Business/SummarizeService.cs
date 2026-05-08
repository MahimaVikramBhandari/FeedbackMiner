using Microsoft.EntityFrameworkCore;
using System.Text;

/// <summary>
/// Service for summarizing feedback analysis and metrics
/// </summary>
public class SummarizeService
{
    private readonly FeedbackDbContext _dbContext;
    private readonly EvaluationMetricsService _metricsService;
    private readonly ReportingService _reportingService;
    private readonly EvaluationNotebookService _notebookService;
    private readonly OpenAIService _openAIService;
    private readonly ILogger<SummarizeService> _logger;

    public SummarizeService(
        FeedbackDbContext dbContext,
        EvaluationMetricsService metricsService,
        ReportingService reportingService,
        EvaluationNotebookService notebookService,
        OpenAIService openAIService,
        ILogger<SummarizeService> logger)
    {
        _dbContext = dbContext;
        _metricsService = metricsService;
        _reportingService = reportingService;
        _notebookService = notebookService;
        _openAIService = openAIService;
        _logger = logger;
    }

    /// <summary>
    /// Get average theme relevance from evaluations
    /// </summary>
    public async Task<SummaryResponse> GetAverageThemeRelevanceAsync()
    {
        try
        {
            var evaluations = await _dbContext.ThemeEvaluations
                .AsNoTracking()
                .ToListAsync();

            if (!evaluations.Any())
            {
                return new SummaryResponse
                {
                    Success = true,
                    Summary = "No theme evaluations found yet.",
                    Data = new { AverageRelevance = 0 }
                };
            }

            double averageRelevance = evaluations.Average(e => e.RelevanceScore);

            return new SummaryResponse
            {
                Success = true,
                Summary = $"The average theme relevance score is {averageRelevance:F2} out of 5.0. This indicates how well the identified themes represent the feedback provided.",
                Data = new
                {
                    AverageRelevance = Math.Round(averageRelevance, 2),
                    TotalEvaluations = evaluations.Count,
                    LowestScore = evaluations.Min(e => e.RelevanceScore),
                    HighestScore = evaluations.Max(e => e.RelevanceScore)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting average theme relevance: {ex.Message}");
            return new SummaryResponse { Success = false, Summary = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Get average cluster similarity score
    /// </summary>
    public async Task<SummaryResponse> GetClusterSimilarityAverageAsync()
    {
        try
        {
            var themeClusters = await _dbContext.ThemeClusters
                .AsNoTracking()
                .ToListAsync();

            if (!themeClusters.Any())
            {
                return new SummaryResponse
                {
                    Success = true,
                    Summary = "No theme clusters found yet.",
                    Data = new { AverageSimilarity = 0 }
                };
            }

            double averageSimilarity = themeClusters.Average(c => c.AverageSimilarity);

            return new SummaryResponse
            {
                Success = true,
                Summary = $"The average cluster similarity score is {averageSimilarity:F2}. Higher scores indicate more cohesive clusters with similar feedback items.",
                Data = new
                {
                    AverageSimilarity = Math.Round(averageSimilarity, 2),
                    TotalClusters = themeClusters.Count,
                    LowestSimilarity = themeClusters.Min(c => c.AverageSimilarity),
                    HighestSimilarity = themeClusters.Max(c => c.AverageSimilarity)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting cluster similarity average: {ex.Message}");
            return new SummaryResponse { Success = false, Summary = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Summarize feedback reports
    /// </summary>
    public async Task<SummaryResponse> SummarizeFeedbackReportsAsync()
    {
        try
        {
            var feedbackCount = await _dbContext.FeedbackItems.CountAsync();
            var processingRuns = await _dbContext.ProcessingRuns.AsNoTracking().ToListAsync();
            var themes = await _dbContext.Themes.AsNoTracking().ToListAsync();

            var summary = new StringBuilder();
            summary.AppendLine($"**Feedback Reports Summary**");
            summary.AppendLine($"Total Feedback Items: {feedbackCount}");
            summary.AppendLine($"Total Processing Runs: {processingRuns.Count}");
            summary.AppendLine($"Total Themes Identified: {themes.Count}");

            if (processingRuns.Any())
            {
                var avgQuality = processingRuns.Average(pr => pr.AverageClusterQuality);
                summary.AppendLine($"Average Cluster Quality: {avgQuality:F2}");
            }

            return new SummaryResponse
            {
                Success = true,
                Summary = summary.ToString(),
                Data = new
                {
                    FeedbackCount = feedbackCount,
                    ProcessingRunsCount = processingRuns.Count,
                    ThemesCount = themes.Count
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error summarizing feedback reports: {ex.Message}");
            return new SummaryResponse { Success = false, Summary = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Summarize weekly digest report
    /// </summary>
    public async Task<SummaryResponse> SummarizeWeeklyDigestAsync()
    {
        try
        {
            var weeklyDigests = await _dbContext.ScheduledDigestRuns
                .AsNoTracking()
                .OrderByDescending(d => d.WeekEnd)
                .FirstOrDefaultAsync();

            if (weeklyDigests == null)
            {
                return new SummaryResponse
                {
                    Success = true,
                    Summary = "No weekly digest reports available yet.",
                    Data = new { AvailableReports = 0 }
                };
            }

            var summary = new StringBuilder();
            summary.AppendLine($"**Most Recent Weekly Digest**");
            summary.AppendLine($"Week: {weeklyDigests.WeekStart:yyyy-MM-dd} to {weeklyDigests.WeekEnd:yyyy-MM-dd}");
            summary.AppendLine($"Feedback Items: {weeklyDigests.FeedbackCount}");
            summary.AppendLine($"New Themes: {weeklyDigests.NewThemesCount}");
            summary.AppendLine($"Critical Action Items: {weeklyDigests.CriticalActionItemsCount}");
            summary.AppendLine($"Status: {weeklyDigests.Status}");

            return new SummaryResponse
            {
                Success = true,
                Summary = summary.ToString(),
                Data = new
                {
                    WeekStart = weeklyDigests.WeekStart,
                    WeekEnd = weeklyDigests.WeekEnd,
                    FeedbackCount = weeklyDigests.FeedbackCount,
                    NewThemesCount = weeklyDigests.NewThemesCount,
                    CriticalActionItemsCount = weeklyDigests.CriticalActionItemsCount
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error summarizing weekly digest: {ex.Message}");
            return new SummaryResponse { Success = false, Summary = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Summarize cluster report
    /// </summary>
    public async Task<SummaryResponse> SummarizeClusterReportAsync()
    {
        try
        {
            var clusters = await _dbContext.ThemeClusters
                .AsNoTracking()
                .Include(c => c.FeedbackItems)
                .ToListAsync();

            if (!clusters.Any())
            {
                return new SummaryResponse
                {
                    Success = true,
                    Summary = "No clusters found yet.",
                    Data = new { TotalClusters = 0 }
                };
            }

            var summary = new StringBuilder();
            summary.AppendLine($"**Cluster Analysis Report**");
            summary.AppendLine($"Total Clusters: {clusters.Count}");
            summary.AppendLine($"Average Cluster Size: {clusters.Average(c => c.FeedbackItems?.Count ?? 0):F1} items");
            summary.AppendLine($"Largest Cluster: {clusters.Max(c => c.FeedbackItems?.Count ?? 0)} items");
            summary.AppendLine($"Smallest Cluster: {clusters.Min(c => c.FeedbackItems?.Count ?? 0)} items");
            summary.AppendLine($"Average Similarity: {clusters.Average(c => c.AverageSimilarity):F2}");

            return new SummaryResponse
            {
                Success = true,
                Summary = summary.ToString(),
                Data = new
                {
                    TotalClusters = clusters.Count,
                    AverageSimilarity = Math.Round(clusters.Average(c => c.AverageSimilarity), 2),
                    TotalFeedbackItems = clusters.Sum(c => c.FeedbackItems?.Count ?? 0)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error summarizing cluster report: {ex.Message}");
            return new SummaryResponse { Success = false, Summary = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Summarize evaluation notebook
    /// </summary>
    public async Task<SummaryResponse> SummarizeEvaluationNotebookAsync()
    {
        try
        {
            var latestEvaluation = await _dbContext.EvaluationRuns
                .AsNoTracking()
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestEvaluation == null)
            {
                return new SummaryResponse
                {
                    Success = true,
                    Summary = "No evaluation notebooks available yet.",
                    Data = new { AvailableEvaluations = 0 }
                };
            }

            var summary = new StringBuilder();
            summary.AppendLine($"**Latest Evaluation Notebook**");
            summary.AppendLine($"Status: {latestEvaluation.Status}");
            summary.AppendLine($"Created: {latestEvaluation.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            summary.AppendLine($"Theme Relevance: {latestEvaluation.AverageThemeRelevanceScore:F2}");
            summary.AppendLine($"Clustering Precision: {latestEvaluation.ClusteringPrecision:F2}");
            summary.AppendLine($"Recommendation Usefulness: {latestEvaluation.AverageRecommendationUsefulnessScore:F2}");

            return new SummaryResponse
            {
                Success = true,
                Summary = summary.ToString(),
                Data = new
                {
                    Status = latestEvaluation.Status,
                    AverageThemeRelevance = Math.Round(latestEvaluation.AverageThemeRelevanceScore, 2),
                    ClusteringPrecision = Math.Round(latestEvaluation.ClusteringPrecision, 2),
                    RecommendationUsefulness = Math.Round(latestEvaluation.AverageRecommendationUsefulnessScore, 2)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error summarizing evaluation notebook: {ex.Message}");
            return new SummaryResponse { Success = false, Summary = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Summarize evaluation history
    /// </summary>
    public async Task<SummaryResponse> SummarizeEvaluationHistoryAsync()
    {
        try
        {
            var evaluations = await _dbContext.EvaluationRuns
                .AsNoTracking()
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .ToListAsync();

            if (!evaluations.Any())
            {
                return new SummaryResponse
                {
                    Success = true,
                    Summary = "No evaluation history available yet.",
                    Data = new { TotalEvaluations = 0 }
                };
            }

            var summary = new StringBuilder();
            summary.AppendLine($"**Evaluation History (Last 10)**");
            summary.AppendLine($"Total Evaluations: {evaluations.Count}");

            var completedEvaluations = evaluations.Where(e => e.Status == "Completed").ToList();
            if (completedEvaluations.Any())
            {
                var avgRelevance = completedEvaluations.Average(e => e.AverageThemeRelevanceScore);
                summary.AppendLine($"Average Theme Relevance: {avgRelevance:F2}");
                summary.AppendLine($"Completed Evaluations: {completedEvaluations.Count}");
            }

            return new SummaryResponse
            {
                Success = true,
                Summary = summary.ToString(),
                Data = new
                {
                    TotalEvaluations = evaluations.Count,
                    CompletedEvaluations = completedEvaluations.Count,
                    PendingEvaluations = evaluations.Count - completedEvaluations.Count
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error summarizing evaluation history: {ex.Message}");
            return new SummaryResponse { Success = false, Summary = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Summarize all feedbacks
    /// </summary>
    public async Task<SummaryResponse> SummarizeAllFeedbacksAsync()
    {
        try
        {
            var feedbackItems = await _dbContext.FeedbackItems
                .AsNoTracking()
                .ToListAsync();

            if (!feedbackItems.Any())
            {
                return new SummaryResponse
                {
                    Success = true,
                    Summary = "No feedback items available yet.",
                    Data = new { TotalFeedback = 0 }
                };
            }

            var sentimentCount = feedbackItems.GroupBy(f => f.SentimentLabel)
                .Select(g => new { Sentiment = g.Key, Count = g.Count() })
                .ToList();

            var summary = new StringBuilder();
            summary.AppendLine($"**Complete Feedback Summary**");
            summary.AppendLine($"Total Feedback Items: {feedbackItems.Count}");
            summary.AppendLine($"Date Range: {feedbackItems.Min(f => f.CreatedOn):yyyy-MM-dd} to {feedbackItems.Max(f => f.CreatedOn):yyyy-MM-dd}");

            foreach (var sentiment in sentimentCount)
            {
                summary.AppendLine($"{sentiment.Sentiment}: {sentiment.Count} items");
            }

            var sources = feedbackItems.GroupBy(f => f.Source)
                .Select(g => new { Source = g.Key, Count = g.Count() })
                .ToList();

            summary.AppendLine($"\n**Sources:**");
            foreach (var source in sources)
            {
                summary.AppendLine($"{source.Source}: {source.Count} items");
            }

            return new SummaryResponse
            {
                Success = true,
                Summary = summary.ToString(),
                Data = new
                {
                    TotalFeedback = feedbackItems.Count,
                    SentimentBreakdown = sentimentCount,
                    SourceBreakdown = sources
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error summarizing all feedbacks: {ex.Message}");
            return new SummaryResponse { Success = false, Summary = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Generate notebook summary
    /// </summary>
    public async Task<SummaryResponse> GenerateNotebookSummaryAsync()
    {
        try
        {
            var processingRuns = await _dbContext.ProcessingRuns
                .AsNoTracking()
                .OrderByDescending(pr => pr.StartedAt)
                .FirstOrDefaultAsync();

            if (processingRuns == null)
            {
                return new SummaryResponse
                {
                    Success = true,
                    Summary = "No processing runs available for notebook generation.",
                    Data = new { CanGenerate = false }
                };
            }

            var summary = new StringBuilder();
            summary.AppendLine($"**Notebook Generation Summary**");
            summary.AppendLine($"Latest Processing Run: {processingRuns.Name}");
            summary.AppendLine($"Feedback Items Processed: {processingRuns.FeedbackItemCount}");
            summary.AppendLine($"Clusters Created: {processingRuns.ClusterCount}");
            summary.AppendLine($"Themes Identified: {processingRuns.ThemeCount}");
            summary.AppendLine($"Embedding Model: {processingRuns.EmbeddingModel}");
            summary.AppendLine($"Clustering Algorithm: {processingRuns.ClusteringAlgorithm}");
            summary.AppendLine($"Average Cluster Quality: {processingRuns.AverageClusterQuality:F2}");
            summary.AppendLine($"Status: {processingRuns.Status}");

            return new SummaryResponse
            {
                Success = true,
                Summary = summary.ToString(),
                Data = new
                {
                    RunName = processingRuns.Name,
                    FeedbackItemCount = processingRuns.FeedbackItemCount,
                    ClusterCount = processingRuns.ClusterCount,
                    ThemeCount = processingRuns.ThemeCount,
                    AverageClusterQuality = Math.Round(processingRuns.AverageClusterQuality, 2)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generating notebook summary: {ex.Message}");
            return new SummaryResponse { Success = false, Summary = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Answer free-form questions related to FeedbackMiner (outside the predefined summarize questions).
    /// </summary>
    public async Task<SummaryResponse> AnswerFeedbackMinerQuestionAsync(string userQuestion)
    {
        try
        {
            var answer = await _openAIService.AskDashboardAssistantAsync(userQuestion);

            return new SummaryResponse
            {
                Success = true,
                Summary = answer,
                Data = new
                {
                    Question = userQuestion
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error answering FeedbackMiner question: {ex.Message}");
            return new SummaryResponse { Success = false, Summary = $"Error: {ex.Message}" };
        }
    }
}

/// <summary>
/// Response model for summarize endpoints
/// </summary>
public class SummaryResponse
{
    public bool Success { get; set; }
    public string Summary { get; set; } = string.Empty;
    public object? Data { get; set; }
}

/// <summary>
/// Request model for summarize endpoints
/// </summary>
public class SummarizeRequest
{
    public string Question { get; set; } = string.Empty;
}
