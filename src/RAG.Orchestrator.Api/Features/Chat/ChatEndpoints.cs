using RAG.Orchestrator.Api.Features.Chat;

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
            return Results.Ok(sessions);
        })
        .WithName("GetChatSessions")
        .WithSummary("Get all chat sessions")
        .WithDescription("Retrieve all chat sessions for the current user");

        group.MapPost("/sessions", async (CreateSessionRequest request, IChatService chatService) =>
        {
            var session = await chatService.CreateSessionAsync(request);
            return Results.Created($"/api/chat/sessions/{session.Id}", session);
        })
        .WithName("CreateChatSession")
        .WithSummary("Create a new chat session")
        .WithDescription("Create a new chat session with an optional title");

        group.MapGet("/sessions/{sessionId}", async (string sessionId, IChatService chatService) =>
        {
            var session = await chatService.GetSessionAsync(sessionId);
            return session != null ? Results.Ok(session) : Results.NotFound();
        })
        .WithName("GetChatSession")
        .WithSummary("Get a specific chat session")
        .WithDescription("Retrieve a chat session by its ID including all messages");

        group.MapPost("/sessions/{sessionId}/messages", async (string sessionId, ChatRequest request, IChatService chatService) =>
        {
            try
            {
                var message = await chatService.SendMessageAsync(sessionId, request);
                return Results.Ok(message);
            }
            catch (ArgumentException)
            {
                return Results.NotFound();
            }
        })
        .WithName("SendMessage")
        .WithSummary("Send a message in a chat session")
        .WithDescription("Send a message to a chat session and receive an AI response");

        group.MapDelete("/sessions/{sessionId}", async (string sessionId, IChatService chatService) =>
        {
            var deleted = await chatService.DeleteSessionAsync(sessionId);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteChatSession")
        .WithSummary("Delete a chat session")
        .WithDescription("Delete a chat session and all its messages");

        return endpoints;
    }
}
