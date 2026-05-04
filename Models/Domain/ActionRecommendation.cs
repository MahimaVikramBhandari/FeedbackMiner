using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ActionRecommendation
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Theme this recommendation is for
    /// </summary>
    public Guid ThemeId { get; set; }

    [ForeignKey(nameof(ThemeId))]
    public Theme Theme { get; set; }

    /// <summary>
    /// Title of the recommended action
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Detailed description of the action
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Action category (Bug Fix, Feature Request, Process Improvement, etc.)
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// Priority level (Low, Medium, High, Critical)
    /// </summary>
    public string Priority { get; set; }

    /// <summary>
    /// Estimated effort (1-5, where 1 is minimal)
    /// </summary>
    public int EstimatedEffort { get; set; }

    /// <summary>
    /// Potential impact score (1-5)
    /// </summary>
    public double ImpactScore { get; set; }

    /// <summary>
    /// Usefulness rating from user feedback (1-5)
    /// </summary>
    public double? UsefulnessRating { get; set; }

    /// <summary>
    /// Implementation status (Proposed, In Progress, Completed, Rejected)
    /// </summary>
    public string Status { get; set; } = "Proposed";

    /// <summary>
    /// Owner or team assigned to this action
    /// </summary>
    public string AssignedTeam { get; set; }

    /// <summary>
    /// JSON array of affected product areas
    /// </summary>
    public string AffectedAreasJson { get; set; }

    /// <summary>
    /// JSON array of customer segments that would benefit
    /// </summary>
    public string BenefitSegmentsJson { get; set; }

    /// <summary>
    /// When recommended
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
