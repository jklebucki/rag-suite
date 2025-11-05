using Microsoft.EntityFrameworkCore;
using RAG.Orchestrator.Api.Common.Constants;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Localization;
using RAG.Orchestrator.Api.Models;
using RAG.Orchestrator.Api.Services;

namespace RAG.Orchestrator.Api.Features.Chat.SessionManagement;

/// <summary>
/// Manages chat sessions for users
/// </summary>
public class SessionManager : ISessionManager
{
    private readonly ChatDbContext _chatDbContext;
    private readonly ILanguageService _languageService;
    private readonly ILlmService _llmService;
    private readonly IGlobalSettingsService _globalSettingsService;
    private readonly ILogger<SessionManager> _logger;

    public SessionManager(
        ChatDbContext chatDbContext,
        ILanguageService languageService,
        ILlmService llmService,
        IGlobalSettingsService globalSettingsService,
        ILogger<SessionManager> logger)
    {
        _chatDbContext = chatDbContext;
        _languageService = languageService;
        _llmService = llmService;
        _globalSettingsService = globalSettingsService;
        _logger = logger;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<UserChatSession> CreateUserSessionAsync(string userId, CreateUserSessionRequest request, CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid().ToString();
        var language = request.Language ?? _languageService.GetDefaultLanguage();
        var normalizedLanguage = _languageService.NormalizeLanguage(language);
        var sessionTitle = request.Title ?? _languageService.GetLocalizedString(
            "session_labels",
            LocalizationKeys.SessionLabels.NewConversation,
            normalizedLanguage);

        var dbSession = new ChatSession
        {
            Id = sessionId,
            UserId = userId,
            Title = sessionTitle,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _chatDbContext.ChatSessions.Add(dbSession);
        await _chatDbContext.SaveChangesAsync(cancellationToken);

        // Initialize with system message for Ollama
        await InitializeSessionWithSystemMessageAsync(sessionId, normalizedLanguage, cancellationToken);

        return new UserChatSession(
            dbSession.Id,
            dbSession.UserId,
            dbSession.Title,
            Array.Empty<UserChatMessage>(),
            dbSession.CreatedAt,
            dbSession.UpdatedAt
        );
    }

    /// <inheritdoc />
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
                m.Metadata,
                m.OllamaContext
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

    /// <inheritdoc />
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

    /// <summary>
    /// Initializes a new chat session with system message for Ollama /api/chat
    /// Note: System message is not stored in database, it's used only internally by LLM API
    /// </summary>
    private async Task InitializeSessionWithSystemMessageAsync(string sessionId, string language, CancellationToken cancellationToken)
    {
        var settings = await _globalSettingsService.GetLlmSettingsAsync();
        if (settings == null || !settings.IsOllama)
            return;

        // Get system message from localized JSON to validate it exists
        var systemMessage = await _llmService.GetSystemMessageAsync(language, cancellationToken);

        if (!string.IsNullOrEmpty(systemMessage))
        {
            // System message is used internally by ChatWithHistoryAsync when includeSystemMessage=true
            // It's not stored in database as it's not part of user-visible conversation history
            _logger.LogDebug("Initialized session {SessionId} with system message available in language: {Language}", sessionId, language);
        }
    }
}

