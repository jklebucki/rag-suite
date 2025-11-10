using FeedbackModel = RAG.Orchestrator.Api.Models.Feedback;

namespace RAG.Orchestrator.Api.Features.Feedback;

public interface IFeedbackService
{
    Task<FeedbackModel> CreateFeedbackAsync(string userId, string? userEmail, CreateFeedbackRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FeedbackModel>> GetFeedbackAsync(DateTime? from, DateTime? to, string? subject, string? userId, CancellationToken cancellationToken = default);
    Task<FeedbackModel?> RespondToFeedbackAsync(Guid feedbackId, string responderId, string? responderEmail, string response, CancellationToken cancellationToken = default);
}

