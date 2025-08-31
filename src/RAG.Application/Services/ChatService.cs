using Microsoft.Extensions.Logging;
using RAG.Domain.Entities;
using RAG.Domain.ValueObjects;
using RAG.Infrastructure.Persistence.Mock;
using RAG.Infrastructure.SemanticKernel;

namespace RAG.Application.Services;

public record CreateSessionRequest(string UserId, string Context);
public record CreateSessionResponse(string SessionId, string UserId, string Context, DateTime CreatedAt);

public record SendMessageRequest(string SessionId, string Message, bool IncludeContext = true);
public record SendMessageResponse(
    string SessionId,
    string MessageId, 
    string Response, 
    List<ContextItem> Context,
    int TokensUsed,
    long ProcessingTimeMs,
    DateTime Timestamp
);

public record ContextItem(string Source, string Content, double Score);

public interface IChatService
{
    Task<CreateSessionResponse> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default);
}

public class ChatService : IChatService
{
    private readonly IChatSessionRepository _repository;
    private readonly ISemanticKernelService _semanticKernel;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IChatSessionRepository repository,
        ISemanticKernelService semanticKernel,
        ILogger<ChatService> logger)
    {
        _repository = repository;
        _semanticKernel = semanticKernel;
        _logger = logger;
    }

    public async Task<CreateSessionResponse> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var sessionId = SessionId.Create();
        var session = ChatSession.Create(sessionId, request.UserId, request.Context);
        
        await _repository.SaveAsync(session, cancellationToken);
        
        _logger.LogInformation("Created new session {SessionId} for user {UserId}", sessionId.Value, request.UserId);
        
        return new CreateSessionResponse(
            sessionId.Value,
            request.UserId,
            request.Context,
            session.CreatedAt
        );
    }

    public async Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        var sessionId = SessionId.Create(request.SessionId);
        var session = await _repository.GetByIdAsync(sessionId, cancellationToken);
        
        if (session == null)
        {
            throw new ArgumentException($"Session {request.SessionId} not found");
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Add user message
        var userMessage = Message.Create(request.Message, MessageRole.User);
        session.AddMessage(userMessage);
        
        // Generate AI response using Semantic Kernel
        var ragResponse = await _semanticKernel.GenerateResponseAsync(request.Message, cancellationToken);
        
        // Add AI response
        var aiMessage = Message.Create(ragResponse.Response, MessageRole.Assistant);
        session.AddMessage(aiMessage);
        
        await _repository.SaveAsync(session, cancellationToken);
        
        stopwatch.Stop();
        
        _logger.LogInformation("Processed message for session {SessionId} in {ElapsedMs}ms", 
            request.SessionId, stopwatch.ElapsedMilliseconds);
        
        return new SendMessageResponse(
            request.SessionId,
            aiMessage.Id.Value,
            ragResponse.Response,
            ragResponse.Context?.Select(c => new ContextItem(c.Source, c.Content, c.Score)).ToList() ?? new List<ContextItem>(),
            ragResponse.TokensUsed,
            stopwatch.ElapsedMilliseconds,
            aiMessage.CreatedAt
        );
    }
}
