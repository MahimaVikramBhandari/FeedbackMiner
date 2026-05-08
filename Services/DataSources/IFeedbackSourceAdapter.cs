/// <summary>
/// Interface for feedback source adapters
/// </summary>
public interface IFeedbackSourceAdapter
{
    /// <summary>
    /// Get the name of this adapter
    /// </summary>
    string GetSourceName();

    /// <summary>
    /// Fetch feedback from the source
    /// </summary>
    Task<List<FeedbackItem>> FetchFeedbackAsync(Dictionary<string, string> credentials, DateTime? since = null);

    /// <summary>
    /// Test the connection to the source
    /// </summary>
    Task<bool> TestConnectionAsync(Dictionary<string, string> credentials);

    /// <summary>
    /// Get required credential fields for this adapter
    /// </summary>
    List<string> GetRequiredCredentials();
}
