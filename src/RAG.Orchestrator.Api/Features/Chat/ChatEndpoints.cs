using RAG.Orchestrator.Api.Features.Chat;
using RAG.Orchestrator.Api.Models;

namespace RAG.Orchestrator.Api.Features.Chat;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/chat")
            .WithTags("Chat")
            .WithOpenApi();

        group.MapGet("/sessions", async (IChatService chatService) =>
        {
            var sessions = await chatService.GetSessionsAsync();
            return sessions.ToApiResponse();
        })
        .WithName("GetChatSessions")
        .WithSummary("Get all chat sessions")
        .WithDescription("Retrieve all chat sessions for the current user");

        group.MapPost("/sessions", async (CreateSessionRequest request, IChatService chatService) =>
        {
            var session = await chatService.CreateSessionAsync(request);
            return session.ToApiCreatedResponse($"/api/chat/sessions/{session.Id}");
        })
        .WithName("CreateChatSession")
        .WithSummary("Create a new chat session")
        .WithDescription("Create a new chat session with an optional title");

        group.MapGet("/sessions/{sessionId}", async (string sessionId, IChatService chatService) =>
        {
            var session = await chatService.GetSessionAsync(sessionId);
            return session != null ? session.ToApiResponse() : ApiResponseExtensions.ToApiNotFoundResponse<ChatSession>();
        })
        .WithName("GetChatSession")
        .WithSummary("Get a specific chat session")
        .WithDescription("Retrieve a chat session by its ID including all messages");

        group.MapPost("/sessions/{sessionId}/messages", async (string sessionId, ChatRequest request, IChatService chatService) =>
        {
            try
            {
                var message = await chatService.SendMessageAsync(sessionId, request);
                return message.ToApiResponse();
            }
            catch (ArgumentException)
            {
                return ApiResponseExtensions.ToApiNotFoundResponse<ChatMessage>("Session not found");
            }
        })
        .WithName("SendMessage")
        .WithSummary("Send a message in a chat session")
        .WithDescription("Send a message to a chat session and receive an AI response. Use 'UseDocumentSearch' parameter to enable/disable document search in knowledge base.");

        group.MapPost("/sessions/{sessionId}/messages/multilingual", async (string sessionId, Models.MultilingualChatRequest request, IChatService chatService) =>
        {
            try
            {
                var response = await chatService.SendMultilingualMessageAsync(sessionId, request);
                return response.ToApiResponse();
            }
            catch (ArgumentException)
            {
                return ApiResponseExtensions.ToApiNotFoundResponse<Models.MultilingualChatResponse>("Session not found");
            }
        })
        .WithName("SendMultilingualMessage")
        .WithSummary("Send a multilingual message in a chat session")
        .WithDescription("Send a message with language detection, translation, and localized response generation. Use 'UseDocumentSearch' parameter to enable/disable document search in knowledge base.");

        group.MapDelete("/sessions/{sessionId}", async (string sessionId, IChatService chatService) =>
        {
            var deleted = await chatService.DeleteSessionAsync(sessionId);
            return deleted ? Results.NoContent() : ApiResponseExtensions.ToApiNotFoundResponse<object>("Session not found");
        })
        .WithName("DeleteChatSession")
        .WithSummary("Delete a chat session")
        .WithDescription("Delete a chat session and all its messages");

        group.MapGet("/health", async (ILlmService llmService) =>
        {
            try
            {
                var isHealthy = await llmService.IsHealthyAsync();
                var status = new
                {
                    LlmService = isHealthy ? "Healthy" : "Unhealthy",
                    Timestamp = DateTime.UtcNow
                };
                return status.ToApiResponse();
            }
            catch (Exception ex)
            {
                var status = new
                {
                    LlmService = "Error",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                };
                return Results.Json(status, statusCode: 503);
            }
        })
        .WithName("ChatHealthCheck")
        .WithSummary("Check chat service health")
        .WithDescription("Check the health status of the LLM service");

        return endpoints;
    }
}
