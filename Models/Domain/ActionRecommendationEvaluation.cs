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

    public DateTime CreatedOn { get; set; }
}
