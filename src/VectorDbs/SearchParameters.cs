namespace AIOChatbot.VectorDbs;

public class SearchParameters
{
    /// <summary>
    /// Number of indexed documents to return.
    /// </summary>
    public int NumberOfResults { get; set; } = 5;

    /// <summary>
    /// Minimum score to return.
    /// </summary>
    public double MinScore { get; set; } = 0.6;
}
