using FluentValidation;
using FeedbackModel = RAG.Orchestrator.Api.Models.Feedback;
using RAG.Security.Models;
using System.Security.Claims;

namespace RAG.Orchestrator.Api.Features.Feedback;

public static class FeedbackEndpoints
{
    public static IEndpointRouteBuilder MapFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/feedback")
            .WithTags("Feedback")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("/", async (
            ClaimsPrincipal user,
            CreateFeedbackRequest request,
            IValidator<CreateFeedbackRequest> validator,
            IFeedbackService service) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var created = await service.CreateFeedbackAsync(userId, userEmail, request);
            return Results.Created($"/api/feedback/{created.Id}", created.ToResponseItem());
        })
        .WithName("CreateFeedback")
        .WithSummary("Submit feedback")
        .WithDescription("Allows any authenticated user to submit feedback about the application.");

        group.MapGet("/", async (
            DateTime? from,
            DateTime? to,
            string? subject,
            string? userId,
            IFeedbackService service) =>
        {
            var feedback = await service.GetFeedbackAsync(from, to, subject, userId);
            return Results.Ok(feedback.Select(f => f.ToResponseItem()));
        })
        .RequireAuthorization(policy => policy.RequireRole(UserRoles.Admin, UserRoles.PowerUser))
        .WithName("GetFeedback")
        .WithSummary("List feedback")
        .WithDescription("Returns feedback submitted by users, optionally filtered by subject, author, or date range.");

        group.MapPost("/{feedbackId:guid}/response", async (
            Guid feedbackId,
            ClaimsPrincipal user,
            RespondFeedbackRequest request,
            IValidator<RespondFeedbackRequest> validator,
            IFeedbackService service) =>
        {
            var responderId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var responderEmail = user.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(responderId))
            {
                return Results.Unauthorized();
            }

            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var updated = await service.RespondToFeedbackAsync(feedbackId, responderId, responderEmail, request.Response);
            if (updated == null)
            {
                return Results.NotFound(new { Message = "Feedback entry not found" });
            }

            return Results.Ok(updated.ToResponseItem());
        })
        .RequireAuthorization(policy => policy.RequireRole(UserRoles.Admin, UserRoles.PowerUser))
        .WithName("RespondToFeedback")
        .WithSummary("Respond to feedback")
        .WithDescription("Allows administrators and power users to respond to a feedback entry.");

        return app;
    }

    private static FeedbackResponseItem ToResponseItem(this FeedbackModel feedback)
    {
        return new FeedbackResponseItem
        {
            Id = feedback.Id,
            UserId = feedback.UserId,
            UserEmail = feedback.UserEmail,
            Subject = feedback.Subject,
            Message = feedback.Message,
            Response = feedback.Response,
            ResponseAuthorEmail = feedback.ResponseAuthorEmail,
            CreatedAt = feedback.CreatedAt,
            UpdatedAt = feedback.UpdatedAt,
            RespondedAt = feedback.RespondedAt
        };
    }
}

