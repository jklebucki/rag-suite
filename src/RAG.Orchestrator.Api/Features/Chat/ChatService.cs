using RAG.Orchestrator.Api.Features.Chat;
using RAG.Orchestrator.Api.Features.Search;

namespace RAG.Orchestrator.Api.Features.Chat;

public interface IChatService
{
    Task<ChatSession[]> GetSessionsAsync(CancellationToken cancellationToken = default);
    Task<ChatSession> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<ChatMessage> SendMessageAsync(string sessionId, ChatRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}

public class ChatService : IChatService
{
    private static readonly List<ChatSession> _mockSessions = new();
    private static readonly Dictionary<string, List<ChatMessage>> _mockMessages = new();

    public Task<ChatSession[]> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_mockSessions.ToArray());
    }

    public Task<ChatSession> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid().ToString();
        var session = new ChatSession(
            sessionId,
            request.Title ?? "New Chat",
            Array.Empty<ChatMessage>(),
            DateTime.Now,
            DateTime.Now
        );
        
        _mockSessions.Add(session);
        _mockMessages[sessionId] = new List<ChatMessage>();
        
        return Task.FromResult(session);
    }

    public Task<ChatSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = _mockSessions.FirstOrDefault(s => s.Id == sessionId);
        if (session == null) return Task.FromResult<ChatSession?>(null);
        
        var messages = _mockMessages.GetValueOrDefault(sessionId, new List<ChatMessage>());
        var sessionWithMessages = session with { Messages = messages.ToArray() };
        return Task.FromResult<ChatSession?>(sessionWithMessages);
    }

    public Task<ChatMessage> SendMessageAsync(string sessionId, ChatRequest request, CancellationToken cancellationToken = default)
    {
        if (!_mockMessages.ContainsKey(sessionId))
            throw new ArgumentException("Session not found", nameof(sessionId));

        // Add user message
        var userMessage = new ChatMessage(
            Guid.NewGuid().ToString(),
            "user",
            request.Message,
            DateTime.Now
        );
        _mockMessages[sessionId].Add(userMessage);

        // Generate mock AI response
        var aiResponse = GenerateMockResponse(request.Message);
        var aiMessage = new ChatMessage(
            Guid.NewGuid().ToString(),
            "assistant",
            aiResponse.Content,
            DateTime.Now.AddSeconds(2),
            aiResponse.Sources
        );
        _mockMessages[sessionId].Add(aiMessage);

        // Update session timestamp
        var sessionIndex = _mockSessions.FindIndex(s => s.Id == sessionId);
        if (sessionIndex >= 0)
        {
            _mockSessions[sessionIndex] = _mockSessions[sessionIndex] with { UpdatedAt = DateTime.Now };
        }

        return Task.FromResult(aiMessage);
    }

    public Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var removed = _mockSessions.RemoveAll(s => s.Id == sessionId) > 0;
        _mockMessages.Remove(sessionId);
        return Task.FromResult(removed);
    }

    private static (string Content, SearchResult[]? Sources) GenerateMockResponse(string userMessage)
    {
        var lowerMessage = userMessage.ToLower();
        
        if (lowerMessage.Contains("oracle") || lowerMessage.Contains("database"))
        {
            return (
                "Based on the Oracle database documentation, I can help you with schema design and query optimization. The current schema includes 15 tables with relationships optimized for RAG applications. Would you like me to explain specific table structures or relationships?",
                new SearchResult[]
                {
                    new("1", "Oracle Database Schema Guide", "Comprehensive guide...", 0.95, "oracle", "schema", new Dictionary<string, object>(), DateTime.Now.AddDays(-10), DateTime.Now.AddDays(-5))
                }
            );
        }
        
        if (lowerMessage.Contains("ifs") || lowerMessage.Contains("user"))
        {
            return (
                "According to the IFS Standard Operating Procedures, user management follows a structured approach with role-based access control. The latest version 2.1 includes enhanced security features and automated provisioning workflows.",
                new SearchResult[]
                {
                    new("2", "IFS SOP Document - User Management", "Standard Operating Procedure...", 0.87, "ifs", "sop", new Dictionary<string, object>(), DateTime.Now.AddDays(-15), DateTime.Now.AddDays(-2))
                }
            );
        }
        
        if (lowerMessage.Contains("process") || lowerMessage.Contains("automation"))
        {
            return (
                "Business process automation can be implemented using workflow engines and approval systems. The guidelines recommend starting with medium complexity processes and gradually scaling up. Would you like specific implementation examples?",
                new SearchResult[]
                {
                    new("3", "Business Process Automation Guidelines", "Guidelines for implementing...", 0.82, "files", "process", new Dictionary<string, object>(), DateTime.Now.AddDays(-7), DateTime.Now.AddDays(-1))
                }
            );
        }
        
        return (
            "I understand your question. Based on the available knowledge base, I can provide information about Oracle databases, IFS systems, and business processes. Could you please be more specific about what you'd like to know?",
            null
        );
    }
}
