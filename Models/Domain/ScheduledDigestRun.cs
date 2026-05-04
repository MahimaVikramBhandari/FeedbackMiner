using System.ComponentModel.DataAnnotations;

public class ScheduledDigestRun
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Week start date (Sunday)
    /// </summary>
    public DateTime WeekStart { get; set; }

    /// <summary>
    /// Week end date (Saturday)
    /// </summary>
    public DateTime WeekEnd { get; set; }

    /// <summary>
    /// Total feedback items received this week
    /// </summary>
    public int FeedbackCount { get; set; }

    /// <summary>
    /// Number of new themes identified this week
    /// </summary>
    public int NewThemesCount { get; set; }

    /// <summary>
    /// Number of high/critical priority action items this week
    /// </summary>
    public int CriticalActionItemsCount { get; set; }

    /// <summary>
    /// Digest generation status
    /// </summary>
    public string Status { get; set; } // "Pending", "Generated", "Sent", "Failed"

    /// <summary>
    /// Serialized digest content (JSON)
    /// </summary>
    public string DigestContentJson { get; set; }

    /// <summary>
    /// Recipients email list (JSON array)
    /// </summary>
    public string RecipientEmailsJson { get; set; }

    /// <summary>
    /// Timestamp when digest was generated
    /// </summary>
    public DateTime? GeneratedAt { get; set; }

    /// <summary>
    /// Timestamp when digest was sent
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Error message if generation/sending failed
    /// </summary>
    public string ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }
}
