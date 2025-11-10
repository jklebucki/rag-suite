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

        var attachments = request.Attachments ?? Array.Empty<FeedbackAttachmentUpload>();

        foreach (var attachment in attachments)
        {
            var data = Convert.FromBase64String(attachment.DataBase64.Trim());

            feedback.Attachments.Add(new Models.FeedbackAttachment
            {
                Id = Guid.NewGuid(),
                FeedbackId = feedback.Id,
                FileName = attachment.FileName.Trim(),
                ContentType = attachment.ContentType.Trim(),
                Data = data,
                CreatedAt = utcNow
            });
        }

        _dbContext.FeedbackEntries.Add(feedback);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return feedback;
    }

    public async Task<IReadOnlyList<FeedbackModel>> GetFeedbackAsync(DateTime? from, DateTime? to, string? subject, string? userId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FeedbackEntries
            .Include(f => f.Attachments)
            .AsQueryable();

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

    public async Task<IReadOnlyList<FeedbackModel>> GetUserFeedbackAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FeedbackEntries
            .Include(f => f.Attachments)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<FeedbackModel?> RespondToFeedbackAsync(Guid feedbackId, string responderId, string? responderEmail, string response, CancellationToken cancellationToken = default)
    {
        var feedback = await _dbContext.FeedbackEntries
            .Include(f => f.Attachments)
            .FirstOrDefaultAsync(f => f.Id == feedbackId, cancellationToken);
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
        feedback.ResponseViewedAt = null;
        feedback.UpdatedAt = utcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return feedback;
    }

    public async Task<FeedbackModel?> MarkFeedbackResponseAsViewedAsync(Guid feedbackId, string userId, CancellationToken cancellationToken = default)
    {
        var feedback = await _dbContext.FeedbackEntries
            .Include(f => f.Attachments)
            .FirstOrDefaultAsync(f => f.Id == feedbackId && f.UserId == userId, cancellationToken);

        if (feedback == null || feedback.RespondedAt == null)
        {
            return null;
        }

        feedback.ResponseViewedAt = DateTime.UtcNow;
        feedback.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return feedback;
    }

    public async Task<bool> DeleteFeedbackAsync(Guid feedbackId, CancellationToken cancellationToken = default)
    {
        var feedback = await _dbContext.FeedbackEntries
            .Include(f => f.Attachments)
            .FirstOrDefaultAsync(f => f.Id == feedbackId, cancellationToken);

        if (feedback == null)
        {
            return false;
        }

        _dbContext.FeedbackEntries.Remove(feedback);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

