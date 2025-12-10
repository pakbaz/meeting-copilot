namespace MeetingCopilot.Contracts.Messages;

/// <summary>
/// Message indicating research results are ready.
/// </summary>
public record ResearchReadyMessage : AgentMessage
{
    /// <summary>
    /// Reference to the original research request message ID.
    /// </summary>
    public string RequestMessageId { get; init; } = default!;
    
    /// <summary>
    /// The original research query.
    /// </summary>
    public string Query { get; init; } = default!;
    
    /// <summary>
    /// The research results.
    /// </summary>
    public List<ResearchResult> Results { get; init; } = new();
    
    /// <summary>
    /// Summary of the research findings.
    /// </summary>
    public string? Summary { get; init; }
    
    /// <summary>
    /// Processing time in milliseconds.
    /// </summary>
    public long ProcessingTimeMs { get; init; }
    
    /// <summary>
    /// Whether the research was successful.
    /// </summary>
    public bool Success { get; init; } = true;
    
    /// <summary>
    /// Error message if research failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

public record ResearchResult
{
    /// <summary>
    /// Title of the research result.
    /// </summary>
    public string Title { get; init; } = default!;
    
    /// <summary>
    /// Snippet/excerpt from the source.
    /// </summary>
    public string Snippet { get; init; } = default!;
    
    /// <summary>
    /// Source URL if available.
    /// </summary>
    public string? Url { get; init; }
    
    /// <summary>
    /// Relevance score (0-1).
    /// </summary>
    public double Relevance { get; init; }
    
    /// <summary>
    /// Source type (web, document, etc.).
    /// </summary>
    public string SourceType { get; init; } = "web";
}
