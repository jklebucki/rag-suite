using RAG.Orchestrator.Api.Features.Chat;
using RAG.Orchestrator.Api.Features.Search;
using RAG.Orchestrator.Api.Localization;
using RAG.Orchestrator.Api.Models;
using RAG.Security.Services;
using System.Text;
using Microsoft.SemanticKernel;

namespace RAG.Orchestrator.Api.Features.Chat;

public interface IUserChatService
{
    Task<UserChatSession[]> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserChatSession> CreateUserSessionAsync(string userId, CreateUserSessionRequest request, CancellationToken cancellationToken = default);
    Task<UserChatSession?> GetUserSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
    Task<UserChatMessage> SendUserMessageAsync(string userId, string sessionId, UserChatRequest request, CancellationToken cancellationToken = default);
    Task<Models.MultilingualChatResponse> SendUserMultilingualMessageAsync(string userId, string sessionId, Models.MultilingualChatRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default);
}

public class UserChatService : IUserChatService
{
    // Store sessions per user
    private static readonly Dictionary<string, List<UserChatSession>> _userSessions = new();
    private static readonly Dictionary<string, Dictionary<string, List<UserChatMessage>>> _userMessages = new();
    
    private readonly Kernel _kernel;
    private readonly ISearchService _searchService;
    private readonly ILanguageService _languageService;
    private readonly ILogger<UserChatService> _logger;
    private readonly IConfiguration _configuration;

    public UserChatService(
        Kernel kernel,
        ISearchService searchService,
        ILanguageService languageService,
        ILogger<UserChatService> logger,
        IConfiguration configuration)
    {
        _kernel = kernel;
        _searchService = searchService;
        _languageService = languageService;
        _logger = logger;
        _configuration = configuration;
    }

    public Task<UserChatSession[]> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var sessions = _userSessions.GetValueOrDefault(userId, new List<UserChatSession>());
        return Task.FromResult(sessions.ToArray());
    }

    public Task<UserChatSession> CreateUserSessionAsync(string userId, CreateUserSessionRequest request, CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid().ToString();
        var language = request.Language ?? _languageService.GetDefaultLanguage();
        var normalizedLanguage = _languageService.NormalizeLanguage(language);
        var sessionTitle = request.Title ?? _languageService.GetLocalizedString("session_labels", "new_conversation", normalizedLanguage);
        
        var session = new UserChatSession(
            sessionId,
            userId,
            sessionTitle,
            Array.Empty<UserChatMessage>(),
            DateTime.Now,
            DateTime.Now
        );
        
        // Initialize user session lists if they don't exist
        if (!_userSessions.ContainsKey(userId))
        {
            _userSessions[userId] = new List<UserChatSession>();
        }
        if (!_userMessages.ContainsKey(userId))
        {
            _userMessages[userId] = new Dictionary<string, List<UserChatMessage>>();
        }
        
        _userSessions[userId].Add(session);
        _userMessages[userId][sessionId] = new List<UserChatMessage>();
        
        return Task.FromResult(session);
    }

    public Task<UserChatSession?> GetUserSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
    {
        var userSessions = _userSessions.GetValueOrDefault(userId, new List<UserChatSession>());
        var session = userSessions.FirstOrDefault(s => s.Id == sessionId);
        
        if (session == null) return Task.FromResult<UserChatSession?>(null);
        
        var userMessages = _userMessages.GetValueOrDefault(userId, new Dictionary<string, List<UserChatMessage>>());
        var messages = userMessages.GetValueOrDefault(sessionId, new List<UserChatMessage>());
        var sessionWithMessages = session with { Messages = messages.ToArray() };
        
        return Task.FromResult<UserChatSession?>(sessionWithMessages);
    }

    public async Task<UserChatMessage> SendUserMessageAsync(string userId, string sessionId, UserChatRequest request, CancellationToken cancellationToken = default)
    {
        // Check if user has access to this session
        var userSessions = _userSessions.GetValueOrDefault(userId, new List<UserChatSession>());
        if (!userSessions.Any(s => s.Id == sessionId))
        {
            throw new ArgumentException("Session not found or access denied", nameof(sessionId));
        }

        var userMessages = _userMessages.GetValueOrDefault(userId, new Dictionary<string, List<UserChatMessage>>());
        if (!userMessages.ContainsKey(sessionId))
        {
            throw new ArgumentException("Session not found", nameof(sessionId));
        }

        var maxMessageLength = _configuration.GetValue<int>("Chat:MaxMessageLength", 2000);
        if (request.Message.Length > maxMessageLength)
        {
            throw new ArgumentException($"Message too long. Maximum length is {maxMessageLength} characters.");
        }

        // Add user message
        var userMessage = new UserChatMessage(
            Guid.NewGuid().ToString(),
            "user",
            request.Message,
            DateTime.Now
        );
        userMessages[sessionId].Add(userMessage);

        try
        {
            // Search for relevant context
            var searchResults = await _searchService.SearchAsync(new Features.Search.SearchRequest(
                request.Message,
                Filters: null,
                Limit: 3,
                Offset: 0
            ), cancellationToken);

            // Build context-aware prompt
            var prompt = BuildContextualPrompt(request.Message, searchResults.Results, userMessages[sessionId]);

            // Generate AI response using Semantic Kernel
            var aiResponse = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var aiResponseContent = aiResponse.GetValue<string>() ?? 
                _languageService.GetLocalizedErrorMessage("generation_failed", _languageService.GetDefaultLanguage());

            var aiMessage = new UserChatMessage(
                Guid.NewGuid().ToString(),
                "assistant",
                aiResponseContent,
                DateTime.Now,
                searchResults.Results.Length > 0 ? searchResults.Results : null
            );
            
            userMessages[sessionId].Add(aiMessage);

            // Update session timestamp
            var sessionIndex = userSessions.FindIndex(s => s.Id == sessionId);
            if (sessionIndex >= 0)
            {
                userSessions[sessionIndex] = userSessions[sessionIndex] with { UpdatedAt = DateTime.Now };
            }

            return aiMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI response for user {UserId} session {SessionId}", userId, sessionId);
            
            var errorMessage = new UserChatMessage(
                Guid.NewGuid().ToString(),
                "assistant",
                _languageService.GetLocalizedErrorMessage("processing_error", _languageService.GetDefaultLanguage()),
                DateTime.Now
            );
            
            userMessages[sessionId].Add(errorMessage);
            return errorMessage;
        }
    }

    public async Task<Models.MultilingualChatResponse> SendUserMultilingualMessageAsync(string userId, string sessionId, Models.MultilingualChatRequest request, CancellationToken cancellationToken = default)
    {
        // Check if user has access to this session
        var userSessions = _userSessions.GetValueOrDefault(userId, new List<UserChatSession>());
        if (!userSessions.Any(s => s.Id == sessionId))
        {
            throw new ArgumentException("Session not found or access denied", nameof(sessionId));
        }

        var userMessages = _userMessages.GetValueOrDefault(userId, new Dictionary<string, List<UserChatMessage>>());
        if (!userMessages.ContainsKey(sessionId))
        {
            throw new ArgumentException("Session not found", nameof(sessionId));
        }

        var maxMessageLength = _configuration.GetValue<int>("Chat:MaxMessageLength", 2000);
        if (request.Message.Length > maxMessageLength)
        {
            throw new ArgumentException($"Message too long. Maximum length is {maxMessageLength} characters.");
        }

        // Detect language if not provided
        var detectedLanguage = string.IsNullOrEmpty(request.Language) 
            ? _languageService.DetectLanguage(request.Message)
            : request.Language;

        var responseLanguage = request.ResponseLanguage ?? detectedLanguage ?? _languageService.GetDefaultLanguage();
        
        // Add user message
        var userMessage = new UserChatMessage(
            Guid.NewGuid().ToString(),
            "user",
            request.Message,
            DateTime.Now,
            null,
            new Dictionary<string, object>
            {
                ["detectedLanguage"] = detectedLanguage ?? "unknown",
                ["originalLanguage"] = detectedLanguage ?? "unknown"
            }
        );
        userMessages[sessionId].Add(userMessage);

        try
        {
            // Search for relevant context
            var searchResults = await _searchService.SearchAsync(new Features.Search.SearchRequest(
                request.Message,
                Filters: null,
                Limit: 3,
                Offset: 0
            ), cancellationToken);

            // Build multilingual context-aware prompt
            var prompt = BuildMultilingualContextualPrompt(
                request.Message, 
                searchResults.Results, 
                userMessages[sessionId],
                detectedLanguage ?? "unknown",
                responseLanguage,
                searchResults.Results.Length > 0
            );

            // Generate AI response
            var aiResponse = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var aiResponseContent = aiResponse.GetValue<string>() ?? 
                _languageService.GetLocalizedErrorMessage("generation_failed", responseLanguage);

            var aiMessage = new UserChatMessage(
                Guid.NewGuid().ToString(),
                "assistant",
                aiResponseContent,
                DateTime.Now,
                searchResults.Results.Length > 0 ? searchResults.Results : null,
                new Dictionary<string, object>
                {
                    ["responseLanguage"] = responseLanguage,
                    ["documentsUsed"] = searchResults.Results.Length
                }
            );
            
            userMessages[sessionId].Add(aiMessage);

            // Update session timestamp
            var sessionIndex = userSessions.FindIndex(s => s.Id == sessionId);
            if (sessionIndex >= 0)
            {
                userSessions[sessionIndex] = userSessions[sessionIndex] with { UpdatedAt = DateTime.Now };
            }

            return new Models.MultilingualChatResponse
            {
                Response = aiMessage.Content,
                SessionId = sessionId,
                DetectedLanguage = detectedLanguage ?? "unknown",
                ResponseLanguage = responseLanguage,
                WasTranslated = false, // TODO: Implement translation logic when needed
                Sources = searchResults.Results.Length > 0 ? searchResults.Results.Select(r => r.Content).ToList() : null,
                ProcessingTimeMs = 0 // TODO: Add timing measurement if needed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating multilingual AI response for user {UserId} session {SessionId}", userId, sessionId);
            
            var errorMessage = new UserChatMessage(
                Guid.NewGuid().ToString(),
                "assistant",
                _languageService.GetLocalizedErrorMessage("processing_error", responseLanguage),
                DateTime.Now
            );
            
            userMessages[sessionId].Add(errorMessage);
            
            return new Models.MultilingualChatResponse
            {
                Response = errorMessage.Content,
                SessionId = sessionId,
                DetectedLanguage = detectedLanguage ?? "unknown",
                ResponseLanguage = responseLanguage,
                WasTranslated = false,
                Sources = null,
                ProcessingTimeMs = 0
            };
        }
    }

    public Task<bool> DeleteUserSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
    {
        var userSessions = _userSessions.GetValueOrDefault(userId, new List<UserChatSession>());
        var removed = userSessions.RemoveAll(s => s.Id == sessionId) > 0;
        
        if (removed)
        {
            var userMessages = _userMessages.GetValueOrDefault(userId, new Dictionary<string, List<UserChatMessage>>());
            userMessages.Remove(sessionId);
        }
        
        return Task.FromResult(removed);
    }

    private static string BuildContextualPrompt(string userMessage, Features.Search.SearchResult[] searchResults, List<UserChatMessage> conversationHistory)
    {
        var promptBuilder = new StringBuilder();
        
        // Add system instruction
        promptBuilder.AppendLine("You are a helpful AI assistant. Answer the user's question based on the provided context and conversation history.");
        
        // Add context from search results if available
        if (searchResults.Length > 0)
        {
            promptBuilder.AppendLine("\nRelevant context:");
            foreach (var result in searchResults)
            {
                promptBuilder.AppendLine($"- {result.Content}");
            }
        }
        
        // Add recent conversation history (last 5 messages)
        var recentMessages = conversationHistory.TakeLast(5).ToArray();
        if (recentMessages.Length > 1) // More than just the current message
        {
            promptBuilder.AppendLine("\nRecent conversation:");
            foreach (var msg in recentMessages.SkipLast(1)) // Skip the current message we're responding to
            {
                promptBuilder.AppendLine($"{msg.Role}: {msg.Content}");
            }
        }
        
        // Add current user message
        promptBuilder.AppendLine($"\nUser: {userMessage}");
        promptBuilder.AppendLine("Assistant:");
        
        return promptBuilder.ToString();
    }
    
    private string BuildMultilingualContextualPrompt(
        string userMessage, 
        Features.Search.SearchResult[] searchResults, 
        List<UserChatMessage> conversationHistory,
        string detectedLanguage,
        string responseLanguage,
        bool documentsAvailable = true)
    {
        var promptBuilder = new StringBuilder();
        
        // Add system instruction with language information
        promptBuilder.AppendLine($"You are a helpful multilingual AI assistant. The user wrote in {detectedLanguage} and expects a response in {responseLanguage}.");
        promptBuilder.AppendLine($"Please respond naturally and fluently in {responseLanguage}.");
        
        if (documentsAvailable && searchResults.Length > 0)
        {
            promptBuilder.AppendLine("\nRelevant context from documents:");
            foreach (var result in searchResults)
            {
                promptBuilder.AppendLine($"- {result.Content}");
            }
        }
        else
        {
            promptBuilder.AppendLine("\nNo relevant documents found. Please provide a helpful response based on your general knowledge.");
        }
        
        // Add recent conversation history
        var recentMessages = conversationHistory.TakeLast(5).ToArray();
        if (recentMessages.Length > 1)
        {
            promptBuilder.AppendLine("\nRecent conversation:");
            foreach (var msg in recentMessages.SkipLast(1))
            {
                promptBuilder.AppendLine($"{msg.Role}: {msg.Content}");
            }
        }
        
        promptBuilder.AppendLine($"\nUser ({detectedLanguage}): {userMessage}");
        promptBuilder.AppendLine($"Assistant ({responseLanguage}):");
        
        return promptBuilder.ToString();
    }
}
