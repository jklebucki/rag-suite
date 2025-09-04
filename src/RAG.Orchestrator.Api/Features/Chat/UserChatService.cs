using RAG.Orchestrator.Api.Features.Chat;
using RAG.Orchestrator.Api.Features.Search;
using RAG.Orchestrator.Api.Localization;
using RAG.Orchestrator.Api.Models;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Data.Models;
using RAG.Security.Services;
using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.EntityFrameworkCore;

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
    private readonly ChatDbContext _chatDbContext;
    private readonly Kernel _kernel;
    private readonly ISearchService _searchService;
    private readonly ILanguageService _languageService;
    private readonly ILogger<UserChatService> _logger;
    private readonly IConfiguration _configuration;

    public UserChatService(
        ChatDbContext chatDbContext,
        Kernel kernel,
        ISearchService searchService,
        ILanguageService languageService,
        ILogger<UserChatService> logger,
        IConfiguration configuration)
    {
        _chatDbContext = chatDbContext;
        _kernel = kernel;
        _searchService = searchService;
        _languageService = languageService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<UserChatSession[]> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var dbSessions = await _chatDbContext.ChatSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(cancellationToken);
            
        return dbSessions.Select(s => new UserChatSession(
            s.Id,
            s.UserId,
            s.Title,
            Array.Empty<UserChatMessage>(), // Messages loaded separately when needed
            s.CreatedAt,
            s.UpdatedAt
        )).ToArray();
    }

    public async Task<UserChatSession> CreateUserSessionAsync(string userId, CreateUserSessionRequest request, CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid().ToString();
        var language = request.Language ?? _languageService.GetDefaultLanguage();
        var normalizedLanguage = _languageService.NormalizeLanguage(language);
        var sessionTitle = request.Title ?? _languageService.GetLocalizedString("session_labels", "new_conversation", normalizedLanguage);
        
        var dbSession = new Data.Models.ChatSession
        {
            Id = sessionId,
            UserId = userId,
            Title = sessionTitle,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _chatDbContext.ChatSessions.Add(dbSession);
        await _chatDbContext.SaveChangesAsync(cancellationToken);
        
        return new UserChatSession(
            dbSession.Id,
            dbSession.UserId,
            dbSession.Title,
            Array.Empty<UserChatMessage>(),
            dbSession.CreatedAt,
            dbSession.UpdatedAt
        );
    }

    public async Task<UserChatSession?> GetUserSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
    {
        var dbSession = await _chatDbContext.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);
        
        if (dbSession == null) return null;
        
        var messages = dbSession.Messages
            .OrderBy(m => m.Timestamp)
            .Select(m => new UserChatMessage(
                m.Id,
                m.Role,
                m.Content,
                m.Timestamp,
                m.Sources,
                m.Metadata
            ))
            .ToArray();
        
        return new UserChatSession(
            dbSession.Id,
            dbSession.UserId,
            dbSession.Title,
            messages,
            dbSession.CreatedAt,
            dbSession.UpdatedAt
        );
    }

    public async Task<UserChatMessage> SendUserMessageAsync(string userId, string sessionId, UserChatRequest request, CancellationToken cancellationToken = default)
    {
        // Check if user has access to this session
        var dbSession = await _chatDbContext.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);
            
        if (dbSession == null)
        {
            throw new ArgumentException("Session not found or access denied", nameof(sessionId));
        }

        var maxMessageLength = _configuration.GetValue<int>("Chat:MaxMessageLength", 2000);
        if (request.Message.Length > maxMessageLength)
        {
            throw new ArgumentException($"Message too long. Maximum length is {maxMessageLength} characters.");
        }

        // Get conversation history for context
        var conversationHistory = await _chatDbContext.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Timestamp)
            .Select(m => new UserChatMessage(
                m.Id,
                m.Role,
                m.Content,
                m.Timestamp,
                m.Sources,
                m.Metadata
            ))
            .ToListAsync(cancellationToken);

        // Add user message to database
        var userDbMessage = new Data.Models.ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            SessionId = sessionId,
            Role = "user",
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        };
        
        _chatDbContext.ChatMessages.Add(userDbMessage);
        await _chatDbContext.SaveChangesAsync(cancellationToken);

        // Add to conversation history for prompt building
        var userMessage = new UserChatMessage(
            userDbMessage.Id,
            userDbMessage.Role,
            userDbMessage.Content,
            userDbMessage.Timestamp
        );
        conversationHistory.Add(userMessage);

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
            var prompt = BuildContextualPrompt(request.Message, searchResults.Results, conversationHistory);
            
            // Debug logging for search results content
            _logger.LogDebug("Search results for prompt: {ResultCount} results", searchResults.Results.Length);
            foreach (var result in searchResults.Results)
            {
                _logger.LogDebug("Search result - Source: {Source}, Content length: {ContentLength}, Content preview: {ContentPreview}", 
                    result.Source, result.Content?.Length ?? 0, 
                    result.Content?.Length > 100 ? result.Content[..100] + "..." : result.Content ?? "NULL");
            }
            _logger.LogDebug("Final prompt length: {PromptLength} characters", prompt.Length);

            // Generate AI response using Semantic Kernel
            var aiResponse = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var aiResponseContent = aiResponse.GetValue<string>() ?? 
                _languageService.GetLocalizedErrorMessage("generation_failed", _languageService.GetDefaultLanguage());

            // Save AI response to database
            var aiDbMessage = new Data.Models.ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = sessionId,
                Role = "assistant",
                Content = aiResponseContent,
                Timestamp = DateTime.UtcNow,
                Sources = searchResults.Results.Length > 0 ? searchResults.Results : null
            };
            
            _chatDbContext.ChatMessages.Add(aiDbMessage);
            
            // Update session timestamp
            dbSession.UpdatedAt = DateTime.UtcNow;
            
            await _chatDbContext.SaveChangesAsync(cancellationToken);

            return new UserChatMessage(
                aiDbMessage.Id,
                aiDbMessage.Role,
                aiDbMessage.Content,
                aiDbMessage.Timestamp,
                aiDbMessage.Sources,
                aiDbMessage.Metadata
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI response for user {UserId} session {SessionId}", userId, sessionId);
            
            // Save error message to database
            var errorDbMessage = new Data.Models.ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = sessionId,
                Role = "assistant",
                Content = _languageService.GetLocalizedErrorMessage("processing_error", _languageService.GetDefaultLanguage()),
                Timestamp = DateTime.UtcNow
            };
            
            _chatDbContext.ChatMessages.Add(errorDbMessage);
            await _chatDbContext.SaveChangesAsync(cancellationToken);
            
            return new UserChatMessage(
                errorDbMessage.Id,
                errorDbMessage.Role,
                errorDbMessage.Content,
                errorDbMessage.Timestamp
            );
        }
    }

    public async Task<Models.MultilingualChatResponse> SendUserMultilingualMessageAsync(string userId, string sessionId, Models.MultilingualChatRequest request, CancellationToken cancellationToken = default)
    {
        // Check if user has access to this session
        var dbSession = await _chatDbContext.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);
            
        if (dbSession == null)
        {
            throw new ArgumentException("Session not found or access denied", nameof(sessionId));
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
        
        // Get conversation history for context
        var conversationHistory = await _chatDbContext.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Timestamp)
            .Select(m => new UserChatMessage(
                m.Id,
                m.Role,
                m.Content,
                m.Timestamp,
                m.Sources,
                m.Metadata
            ))
            .ToListAsync(cancellationToken);

        // Add user message to database
        var userDbMessage = new Data.Models.ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            SessionId = sessionId,
            Role = "user",
            Content = request.Message,
            Timestamp = DateTime.UtcNow,
            Metadata = new Dictionary<string, object>
            {
                ["detectedLanguage"] = detectedLanguage ?? "unknown",
                ["originalLanguage"] = detectedLanguage ?? "unknown"
            }
        };
        
        _chatDbContext.ChatMessages.Add(userDbMessage);
        await _chatDbContext.SaveChangesAsync(cancellationToken);

        // Add to conversation history for prompt building
        var userMessage = new UserChatMessage(
            userDbMessage.Id,
            userDbMessage.Role,
            userDbMessage.Content,
            userDbMessage.Timestamp,
            null,
            userDbMessage.Metadata
        );
        conversationHistory.Add(userMessage);

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
                conversationHistory,
                detectedLanguage ?? "unknown",
                responseLanguage,
                searchResults.Results.Length > 0
            );

            // Debug logging for multilingual search results content
            _logger.LogDebug("Multilingual search results for prompt: {ResultCount} results", searchResults.Results.Length);
            foreach (var result in searchResults.Results)
            {
                _logger.LogDebug("Multilingual search result - Source: {Source}, Content length: {ContentLength}, Content preview: {ContentPreview}", 
                    result.Source, result.Content?.Length ?? 0, 
                    result.Content?.Length > 100 ? result.Content[..100] + "..." : result.Content ?? "NULL");
            }
            _logger.LogDebug("Final multilingual prompt length: {PromptLength} characters", prompt.Length);

            // Generate AI response
            var aiResponse = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var aiResponseContent = aiResponse.GetValue<string>() ?? 
                _languageService.GetLocalizedErrorMessage("generation_failed", responseLanguage);

            // Save AI response to database
            var aiDbMessage = new Data.Models.ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = sessionId,
                Role = "assistant",
                Content = aiResponseContent,
                Timestamp = DateTime.UtcNow,
                Sources = searchResults.Results.Length > 0 ? searchResults.Results : null,
                Metadata = new Dictionary<string, object>
                {
                    ["responseLanguage"] = responseLanguage,
                    ["documentsUsed"] = searchResults.Results.Length
                }
            };
            
            _chatDbContext.ChatMessages.Add(aiDbMessage);
            
            // Update session timestamp
            dbSession.UpdatedAt = DateTime.UtcNow;
            
            await _chatDbContext.SaveChangesAsync(cancellationToken);

            return new Models.MultilingualChatResponse
            {
                Response = aiDbMessage.Content,
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
            
            // Save error message to database
            var errorDbMessage = new Data.Models.ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = sessionId,
                Role = "assistant",
                Content = _languageService.GetLocalizedErrorMessage("processing_error", responseLanguage),
                Timestamp = DateTime.UtcNow
            };
            
            _chatDbContext.ChatMessages.Add(errorDbMessage);
            await _chatDbContext.SaveChangesAsync(cancellationToken);
            
            return new Models.MultilingualChatResponse
            {
                Response = errorDbMessage.Content,
                SessionId = sessionId,
                DetectedLanguage = detectedLanguage ?? "unknown",
                ResponseLanguage = responseLanguage,
                WasTranslated = false,
                Sources = null,
                ProcessingTimeMs = 0
            };
        }
    }

    public async Task<bool> DeleteUserSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
    {
        var dbSession = await _chatDbContext.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, cancellationToken);
        
        if (dbSession == null)
        {
            return false;
        }
        
        // Delete the session (messages will be deleted automatically due to cascade delete)
        _chatDbContext.ChatSessions.Remove(dbSession);
        await _chatDbContext.SaveChangesAsync(cancellationToken);
        
        return true;
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
