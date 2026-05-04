using System.Text.Json;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Service for generating evaluation notebooks for analysis
/// </summary>
public class EvaluationNotebookService
{
    private readonly FeedbackDbContext _dbContext;
    private readonly EvaluationMetricsService _evaluationService;

    public EvaluationNotebookService(
        FeedbackDbContext dbContext,
        EvaluationMetricsService evaluationService)
    {
        _dbContext = dbContext;
        _evaluationService = evaluationService;
    }

    /// <summary>
    /// Generate evaluation notebook for a processing run
    /// </summary>
    public async Task<EvaluationNotebook> GenerateNotebookAsync(Guid processingRunId)
    {
        var evaluationRun = _dbContext.EvaluationRuns
            .FirstOrDefault(er => er.ProcessingRunId == processingRunId);

        if (evaluationRun == null)
            throw new InvalidOperationException($"No evaluation found for processing run {processingRunId}");

        var notebook = new EvaluationNotebook
        {
            Id = Guid.NewGuid(),
            ProcessingRunId = processingRunId,
            EvaluationRunId = evaluationRun.Id,
            GeneratedAt = DateTime.UtcNow,
            Title = $"Evaluation Report - Run {processingRunId}",
            Sections = new List<NotebookSection>()
        };

        // Executive Summary
        AddExecutiveSummary(notebook, evaluationRun);

        // Quality Metrics
        AddQualityMetricsSection(notebook, evaluationRun);

        // Theme Analysis
        AddThemeAnalysisSection(notebook, evaluationRun);

        // Recommendation Analysis
        AddRecommendationAnalysisSection(notebook, evaluationRun);

        // Clustering Quality
        AddClusteringQualitySection(notebook, evaluationRun);

        // Trends and Insights
        await AddTrendsAndInsightsSection(notebook, evaluationRun);

        // Recommendations for Improvement
        AddImprovementRecommendations(notebook, evaluationRun);

        return notebook;
    }

    private void AddExecutiveSummary(EvaluationNotebook notebook, EvaluationRun evaluationRun)
    {
        var summary = _evaluationService.GetEvaluationSummary(evaluationRun);

        var section = new NotebookSection
        {
            Title = "Executive Summary",
            Content = new Dictionary<string, object>
            {
                { "OverallQualityScore", $"{evaluationRun.OverallQualityScore:F1}/100" },
                { "Status", summary.GetAssessment() },
                { "EvaluatedAt", evaluationRun.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A" },
                { "ThemesEvaluated", _dbContext.ThemeEvaluations.Count(te => te.EvaluationRunId == evaluationRun.Id) },
                { "RecommendationsEvaluated", _dbContext.ActionRecommendationEvaluations.Count(are => are.EvaluationRunId == evaluationRun.Id) }
            }
        };

        notebook.Sections.Add(section);
    }

    private void AddQualityMetricsSection(EvaluationNotebook notebook, EvaluationRun evaluationRun)
    {
        var summary = _evaluationService.GetEvaluationSummary(evaluationRun);

        var metrics = new Dictionary<string, object>
        {
            { "ThemeRelevance", new {
                Score = summary.AverageThemeRelevance.Score,
                Target = summary.AverageThemeRelevance.Target,
                Status = summary.AverageThemeRelevance.MetThreshold ? "✓ PASS" : "✗ FAIL",
                MetPercentage = $"{summary.AverageThemeRelevance.MetPercentage:F1}%"
            }},
            { "ClusteringPrecision", new {
                Score = $"{summary.ClusteringPrecision.Score:F3}",
                Target = summary.ClusteringPrecision.Target,
                Status = summary.ClusteringPrecision.MetThreshold ? "✓ PASS" : "✗ FAIL",
                MetPercentage = $"{summary.ClusteringPrecision.MetPercentage:F1}%"
            }},
            { "RecommendationUsefulness", new {
                Score = summary.RecommendationUsefulness.Score,
                Target = summary.RecommendationUsefulness.Target,
                Status = summary.RecommendationUsefulness.MetThreshold ? "✓ PASS" : "✗ FAIL",
                MetPercentage = $"{summary.RecommendationUsefulness.MetPercentage:F1}%"
            }}
        };

        var section = new NotebookSection
        {
            Title = "Quality Metrics",
            Content = metrics
        };

        notebook.Sections.Add(section);
    }

    private void AddThemeAnalysisSection(EvaluationNotebook notebook, EvaluationRun evaluationRun)
    {
        var themeEvaluations = _dbContext.ThemeEvaluations
            .Where(te => te.EvaluationRunId == evaluationRun.Id)
            .Include(te => te.Theme)
            .ToList();

        var themeAnalysis = new Dictionary<string, object>
        {
            { "TotalThemesEvaluated", themeEvaluations.Count },
            { "ThemesMeetingRelevance", themeEvaluations.Count(te => te.MetRelevanceThreshold) },
            { "AverageRelevanceScore", $"{themeEvaluations.Average(te => te.RelevanceScore):F2}/5.0" },
            { "TopThemesByFeedback", themeEvaluations
                .OrderByDescending(te => te.EstimatedAffectedCustomers)
                .Take(5)
                .Select(te => new {
                    Label = te.Theme.Label,
                    FeedbackCount = te.EstimatedAffectedCustomers,
                    Relevance = $"{te.RelevanceScore:F1}/5.0",
                    MetThreshold = te.MetRelevanceThreshold ? "Yes" : "No"
                })
                .ToList() }
        };

        var section = new NotebookSection
        {
            Title = "Theme Analysis",
            Content = themeAnalysis
        };

        notebook.Sections.Add(section);
    }

    private void AddRecommendationAnalysisSection(EvaluationNotebook notebook, EvaluationRun evaluationRun)
    {
        var recEvaluations = _dbContext.ActionRecommendationEvaluations
            .Where(are => are.EvaluationRunId == evaluationRun.Id)
            .Include(are => are.ActionRecommendation)
            .ToList();

        var recAnalysis = new Dictionary<string, object>
        {
            { "TotalRecommendationsEvaluated", recEvaluations.Count },
            { "RecommendationsMeetingUsefulness", recEvaluations.Count(are => are.MetUsefulnessThreshold) },
            { "AverageUsefulnessScore", $"{recEvaluations.Average(are => are.UsefulnessScore):F2}/5.0" },
            { "AverageFeasibilityScore", $"{recEvaluations.Average(are => are.FeasibilityScore):F2}/5.0" },
            { "TopRecommendationsByUsefulness", recEvaluations
                .OrderByDescending(are => are.UsefulnessScore)
                .Take(5)
                .Select(are => new {
                    Title = are.ActionRecommendation.Title,
                    Usefulness = $"{are.UsefulnessScore:F1}/5.0",
                    Feasibility = $"{are.FeasibilityScore:F1}/5.0",
                    Priority = are.ActionRecommendation.Priority
                })
                .ToList() }
        };

        var section = new NotebookSection
        {
            Title = "Recommendation Analysis",
            Content = recAnalysis
        };

        notebook.Sections.Add(section);
    }

    private void AddClusteringQualitySection(EvaluationNotebook notebook, EvaluationRun evaluationRun)
    {
        var clusteringAnalysis = new Dictionary<string, object>
        {
            { "AverageSilhouetteScore", $"{evaluationRun.AverageSilhouetteScore:F3}" },
            { "ClusteringPrecision", $"{evaluationRun.ClusteringPrecision:F3}" },
            { "DuplicateDetectionRate", $"{(evaluationRun.DuplicateDetectionRate * 100):F1}%" }
        };

        var section = new NotebookSection
        {
            Title = "Clustering Quality",
            Content = clusteringAnalysis
        };

        notebook.Sections.Add(section);
    }

    private async Task AddTrendsAndInsightsSection(EvaluationNotebook notebook, EvaluationRun evaluationRun)
    {
        // Get previous evaluation runs to compare trends
        var previousEvaluations = _dbContext.EvaluationRuns
            .Where(er => er.CreatedAt < evaluationRun.CreatedAt)
            .OrderByDescending(er => er.CreatedAt)
            .Take(5)
            .ToList();

        var trends = new Dictionary<string, object>();

        if (previousEvaluations.Any())
        {
            var latestPrevious = previousEvaluations.First();
            trends.Add("RelevanceScoreTrend", CalculateTrend(latestPrevious.AverageThemeRelevanceScore, evaluationRun.AverageThemeRelevanceScore));
            trends.Add("UsefulnessTrend", CalculateTrend(latestPrevious.AverageRecommendationUsefulnessScore, evaluationRun.AverageRecommendationUsefulnessScore));
            trends.Add("ClusteringPrecisionTrend", CalculateTrend(latestPrevious.ClusteringPrecision, evaluationRun.ClusteringPrecision));
        }

        trends.Add("KeyInsights", new[] {
            "Theme relevance is a primary quality indicator",
            "Clustering precision improves with larger feedback sets",
            "Action recommendation usefulness correlates with theme relevance"
        });

        var section = new NotebookSection
        {
            Title = "Trends and Insights",
            Content = trends
        };

        notebook.Sections.Add(section);
    }

    private void AddImprovementRecommendations(EvaluationNotebook notebook, EvaluationRun evaluationRun)
    {
        var improvements = new List<string>();

        if (evaluationRun.AverageThemeRelevanceScore < 4.0)
            improvements.Add("Increase theme labeling accuracy by using more feedback examples in GPT prompts");

        if (evaluationRun.ClusteringPrecision < 0.8)
            improvements.Add("Adjust similarity threshold for better clustering precision");

        if (evaluationRun.AverageRecommendationUsefulnessScore < 4.0)
            improvements.Add("Refine action recommendation generation to better address customer feedback");

        if (!improvements.Any())
            improvements.Add("All quality metrics are meeting targets. Consider running more frequent evaluations to maintain quality.");

        var section = new NotebookSection
        {
            Title = "Recommendations for Improvement",
            Content = new Dictionary<string, object>
            {
                { "ActionItems", improvements }
            }
        };

        notebook.Sections.Add(section);
    }

    private string CalculateTrend(double previous, double current)
    {
        var change = ((current - previous) / previous) * 100;
        if (Math.Abs(change) < 1)
            return $"Stable (↔ {change:F1}%)";
        else if (change > 0)
            return $"Improving (↑ +{change:F1}%)";
        else
            return $"Declining (↓ {change:F1}%)";
    }

    /// <summary>
    /// Export notebook as JSON
    /// </summary>
    public string ExportAsJson(EvaluationNotebook notebook)
    {
        return JsonSerializer.Serialize(notebook, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Export notebook as HTML
    /// </summary>
    public string ExportAsHtml(EvaluationNotebook notebook)
    {
        var html = new System.Text.StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html><head>");
        html.AppendLine("<meta charset=\"utf-8\">");
        html.AppendLine("<title>" + notebook.Title + "</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }");
        html.AppendLine(".notebook { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
        html.AppendLine("h1 { color: #333; border-bottom: 3px solid #007bff; padding-bottom: 10px; }");
        html.AppendLine("h2 { color: #0056b3; margin-top: 30px; }");
        html.AppendLine(".metric { display: inline-block; margin: 10px 20px 10px 0; padding: 10px 15px; background: #f0f0f0; border-radius: 4px; }");
        html.AppendLine(".pass { color: #28a745; font-weight: bold; }");
        html.AppendLine(".fail { color: #dc3545; font-weight: bold; }");
        html.AppendLine(".section { margin: 20px 0; }");
        html.AppendLine("</style>");
        html.AppendLine("</head><body>");
        html.AppendLine("<div class=\"notebook\">");
        html.AppendLine($"<h1>{notebook.Title}</h1>");
        html.AppendLine($"<p><small>Generated: {notebook.GeneratedAt:yyyy-MM-dd HH:mm:ss}</small></p>");

        foreach (var section in notebook.Sections)
        {
            html.AppendLine($"<div class=\"section\">");
            html.AppendLine($"<h2>{section.Title}</h2>");
            html.AppendLine(RenderContent(section.Content));
            html.AppendLine("</div>");
        }

        html.AppendLine("</div>");
        html.AppendLine("</body></html>");

        return html.ToString();
    }

    private string RenderContent(Dictionary<string, object> content)
    {
        var result = new System.Text.StringBuilder();
        result.AppendLine("<dl>");

        foreach (var item in content)
        {
            result.AppendLine($"<dt><strong>{item.Key}</strong></dt>");

            if (item.Value is Dictionary<string, object> dict)
            {
                result.AppendLine("<dd>");
                foreach (var subItem in dict)
                {
                    result.AppendLine($"<div class=\"metric\">{subItem.Key}: {subItem.Value}</div>");
                }
                result.AppendLine("</dd>");
            }
            else if (item.Value is List<object> list)
            {
                result.AppendLine("<dd><ul>");
                foreach (var listItem in list)
                {
                    result.AppendLine($"<li>{JsonSerializer.Serialize(listItem)}</li>");
                }
                result.AppendLine("</ul></dd>");
            }
            else if (item.Value is string[] arr)
            {
                result.AppendLine("<dd><ul>");
                foreach (var arrItem in arr)
                {
                    result.AppendLine($"<li>{arrItem}</li>");
                }
                result.AppendLine("</ul></dd>");
            }
            else
            {
                result.AppendLine($"<dd>{item.Value}</dd>");
            }
        }

        result.AppendLine("</dl>");
        return result.ToString();
    }
}

/// <summary>
/// Evaluation notebook structure
/// </summary>
public class EvaluationNotebook
{
    public Guid Id { get; set; }
    public Guid ProcessingRunId { get; set; }
    public Guid EvaluationRunId { get; set; }
    public string Title { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<NotebookSection> Sections { get; set; } = new List<NotebookSection>();
}

/// <summary>
/// Notebook section
/// </summary>
public class NotebookSection
{
    public string Title { get; set; }
    public Dictionary<string, object> Content { get; set; }
}
