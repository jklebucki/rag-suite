namespace RAG.Orchestrator.Api.Features.Feedback;

public class CreateFeedbackRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class RespondFeedbackRequest
{
    public string Response { get; set; } = string.Empty;
}

public class FeedbackResponseItem
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Response { get; set; }
    public string? ResponseAuthorEmail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

