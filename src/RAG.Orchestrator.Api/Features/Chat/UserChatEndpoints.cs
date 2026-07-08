using FluentValidation;
using RAG.Abstractions.Common.Api;
using RAG.Orchestrator.Api.Common.Api;
using RAG.Orchestrator.Api.Features.Chat.Attachments;
using RAG.Orchestrator.Api.Models;
using System.Security.Claims;

namespace RAG.Orchestrator.Api.Features.Chat;

public static class UserChatEndpoints
{
    public static IEndpointRouteBuilder MapUserChatEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/user-chat")
            .WithTags("User Chat")
            .RequireAuthorization(); // Require authentication for all endpoints

        group.MapGet("/sessions", async (ClaimsPrincipal user, IUserChatService chatService) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var sessions = await chatService.GetUserSessionsAsync(userId);
            return sessions.ToApiResponse();
        })
        .WithName("GetUserChatSessions")
        .WithSummary("Get all chat sessions for the current user")
        .WithDescription("Retrieve all chat sessions belonging to the authenticated user");

        group.MapPost("/sessions", async (
            ClaimsPrincipal user,
            CreateUserSessionRequest request,
            IUserChatService chatService,
            IValidator<CreateUserSessionRequest> validator) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var session = await chatService.CreateUserSessionAsync(userId, request);
            return session.ToApiCreatedResponse($"/api/user-chat/sessions/{session.Id}");
        })
        .WithName("CreateUserChatSession")
        .WithSummary("Create a new chat session for the current user")
        .WithDescription("Create a new chat session with an optional title for the authenticated user");

        group.MapGet("/sessions/{sessionId}", async (string sessionId, ClaimsPrincipal user, IUserChatService chatService) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var session = await chatService.GetUserSessionAsync(userId, sessionId);
            return session != null ? session.ToApiResponse() : ApiResponseExtensions.ToApiNotFoundResponse<UserChatSession>();
        })
        .WithName("GetUserChatSession")
        .WithSummary("Get a specific chat session for the current user")
        .WithDescription("Retrieve a chat session by its ID if it belongs to the authenticated user");

        group.MapPost("/sessions/{sessionId}/messages/multilingual", async (
            string sessionId,
            MultilingualChatRequest request,
            ClaimsPrincipal user,
            IUserChatService chatService,
            IValidator<MultilingualChatRequest> validator) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            try
            {
                var response = await chatService.SendUserMultilingualMessageAsync(userId, sessionId, request);
                return response.ToApiResponse();
            }
            catch (ArgumentException)
            {
                return ApiResponseExtensions.ToApiNotFoundResponse<MultilingualChatResponse>("Session not found or access denied");
            }
            catch (ChatAttachmentException ex)
            {
                return ToChatAttachmentError(ex);
            }
        })
        .WithName("SendUserMultilingualMessage")
        .WithSummary("Send a multilingual message in a user's chat session")
        .WithDescription("Send a message with language detection, translation, and localized response generation for the authenticated user. Use 'UseDocumentSearch' parameter to enable/disable document search in knowledge base.");

        group.MapGet("/sessions/{sessionId}/context", async (
            string sessionId,
            ClaimsPrincipal user,
            IChatAttachmentService attachmentService,
            CancellationToken cancellationToken) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var context = await attachmentService.GetContextAsync(userId, sessionId, cancellationToken);
            return context != null
                ? context.ToApiResponse()
                : ApiResponseExtensions.ToApiNotFoundResponse<ChatContextUsageResponse>("Session not found or access denied");
        })
        .WithName("GetUserChatContext")
        .WithSummary("Get chat session context usage")
        .WithDescription("Returns session context usage and temporary draft attachments for the authenticated user.");

        group.MapPost("/sessions/{sessionId}/attachments", async (
            string sessionId,
            HttpRequest httpRequest,
            ClaimsPrincipal user,
            IChatAttachmentService attachmentService,
            CancellationToken cancellationToken) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            if (!httpRequest.HasFormContentType)
            {
                return ApiResponseExtensions.ToApiErrorResponse<object>("Multipart form data is required.", new[] { "INVALID_CONTENT_TYPE" });
            }

            try
            {
                var form = await httpRequest.ReadFormAsync(cancellationToken);
                var response = await attachmentService.UploadAsync(userId, sessionId, form.Files, cancellationToken);
                return response.ToApiResponse();
            }
            catch (ChatAttachmentException ex)
            {
                return ToChatAttachmentError(ex);
            }
        })
        .WithName("UploadUserChatAttachments")
        .WithSummary("Upload temporary text attachments for a chat session")
        .WithDescription("Uploads text-only attachments to temporary server-side storage and validates attachment/session context limits.");

        group.MapDelete("/sessions/{sessionId}/attachments/{attachmentId}", async (
            string sessionId,
            string attachmentId,
            ClaimsPrincipal user,
            IChatAttachmentService attachmentService,
            CancellationToken cancellationToken) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var deleted = await attachmentService.DeleteAsync(userId, sessionId, attachmentId, cancellationToken);
            return deleted ? Results.NoContent() : ApiResponseExtensions.ToApiNotFoundResponse<object>("Attachment not found or access denied");
        })
        .WithName("DeleteUserChatAttachment")
        .WithSummary("Delete a temporary chat attachment")
        .WithDescription("Removes one temporary attachment from a user's chat draft.");

        group.MapDelete("/sessions/{sessionId}", async (string sessionId, ClaimsPrincipal user, IUserChatService chatService) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var deleted = await chatService.DeleteUserSessionAsync(userId, sessionId);
            return deleted ? Results.NoContent() : ApiResponseExtensions.ToApiNotFoundResponse<object>("Session not found or access denied");
        })
        .WithName("DeleteUserChatSession")
        .WithSummary("Delete a user's chat session")
        .WithDescription("Delete a chat session and all its messages if it belongs to the authenticated user");

        return endpoints;
    }

    private static IResult ToChatAttachmentError(ChatAttachmentException ex)
    {
        var response = new ApiResponse<object>(default!, false, ex.Message, new[] { ex.Code });
        return ex.Code switch
        {
            "SESSION_NOT_FOUND" => Results.NotFound(response),
            "SESSION_CONTEXT_LIMIT_EXCEEDED" or "ATTACHMENT_CONTEXT_LIMIT_EXCEEDED" => Results.Json(response, statusCode: StatusCodes.Status409Conflict),
            _ => Results.BadRequest(response)
        };
    }
}
