using Microsoft.EntityFrameworkCore;
using RAG.Orchestrator.Api.Data;
using FeedbackModel = RAG.Orchestrator.Api.Models.Feedback;

namespace RAG.Orchestrator.Api.Features.Feedback;

public class FeedbackService : IFeedbackService
{
    private readonly ChatDbContext _dbContext;

    public FeedbackService(ChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FeedbackModel> CreateFeedbackAsync(string userId, string? userEmail, CreateFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        var feedback = new FeedbackModel
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserEmail = userEmail,
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        _dbContext.FeedbackEntries.Add(feedback);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return feedback;
    }

    public async Task<IReadOnlyList<FeedbackModel>> GetFeedbackAsync(DateTime? from, DateTime? to, string? subject, string? userId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FeedbackEntries.AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(f => f.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(f => f.CreatedAt <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(subject))
        {
            var normalizedSubject = subject.Trim();
            query = query.Where(f => EF.Functions.ILike(f.Subject, $"%{normalizedSubject}%"));
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(f => f.UserId == userId);
        }

        return await query
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<FeedbackModel?> RespondToFeedbackAsync(Guid feedbackId, string responderId, string? responderEmail, string response, CancellationToken cancellationToken = default)
    {
        var feedback = await _dbContext.FeedbackEntries.FirstOrDefaultAsync(f => f.Id == feedbackId, cancellationToken);
        if (feedback == null)
        {
            return null;
        }

        var utcNow = DateTime.UtcNow;
        feedback.Response = response.Trim();
        feedback.ResponseAuthorEmail = string.IsNullOrWhiteSpace(responderEmail)
            ? responderId
            : responderEmail.Trim();
        feedback.RespondedAt = utcNow;
        feedback.UpdatedAt = utcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return feedback;
    }
}

