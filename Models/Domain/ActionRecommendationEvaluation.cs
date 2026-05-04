using System.ComponentModel.DataAnnotations;

public class ActionRecommendationEvaluation
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the action recommendation being evaluated
    /// </summary>
    public Guid ActionRecommendationId { get; set; }
    public ActionRecommendation ActionRecommendation { get; set; }

    /// <summary>
    /// Reference to the evaluation run
    /// </summary>
    public Guid EvaluationRunId { get; set; }
    public EvaluationRun EvaluationRun { get; set; }

    /// <summary>
    /// Usefulness score (1-5 scale)
    /// </summary>
    public double UsefulnessScore { get; set; }

    /// <summary>
    /// Whether this recommendation met the usefulness >= 4.0 threshold
    /// </summary>
    public bool MetUsefulnessThreshold { get; set; }

    /// <summary>
    /// Feasibility assessment (1-5 scale)
    /// </summary>
    public double FeasibilityScore { get; set; }

    /// <summary>
    /// Expected business impact (Low, Medium, High, Critical)
    /// </summary>
    public string ExpectedImpact { get; set; }

    /// <summary>
    /// Implementation timeline estimate in days
    /// </summary>
    public int EstimatedTimelineDays { get; set; }

    /// <summary>
    /// Recommendation status
    /// </summary>
    public string Status { get; set; } // "Pending", "Accepted", "Rejected", "InProgress", "Completed"

    /// <summary>
    /// Reviewer feedback
    /// </summary>
    public string ReviewNotes { get; set; }

    /// <summary>
    /// Actual effort spent when implemented
    /// </summary>
    public int? ActualEffortSpent { get; set; }

    /// <summary>
    /// Implementation outcome feedback
    /// </summary>
    public string ImplementationFeedback { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? ImplementedAt { get; set; }
}
