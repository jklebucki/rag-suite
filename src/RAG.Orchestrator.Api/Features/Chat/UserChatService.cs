using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using RAG.Abstractions.Search;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Localization;
using RAG.Orchestrator.Api.Models.Configuration;
using RAG.Orchestrator.Api.Services;
using RAG.Orchestrator.Api.Models;
using RAG.Security.Data;
using System.IO;
using System.Text;

namespace RAG.Orchestrator.Api.Features.Chat;

public class UserChatService : IUserChatService
{
    private readonly ChatDbContext _chatDbContext;
    private readonly SecurityDbContext _securityDbContext;
    private readonly Kernel _kernel;
    private readonly ISearchService _searchService;
    private readonly ILanguageService _languageService;
    private readonly ILogger<UserChatService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ILlmService _llmService;
    private readonly IGlobalSettingsService _globalSettingsService;

    public UserChatService(
        ChatDbContext chatDbContext,
        SecurityDbContext securityDbContext,
        Kernel kernel,
        ISearchService searchService,
        ILanguageService languageService,
        ILogger<UserChatService> logger,
        IConfiguration configuration,
        ILlmService llmService,
        IGlobalSettingsService globalSettingsService)
    {
        _chatDbContext = chatDbContext;
        _securityDbContext = securityDbContext;
        _kernel = kernel;
        _searchService = searchService;
        _languageService = languageService;
        _logger = logger;
        _configuration = configuration;
        _llmService = llmService;
        _globalSettingsService = globalSettingsService;
    }

    private async Task<(string? FirstName, string? LastName, string? Email, string? Role)> GetUserInfoAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _securityDbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
                return (null, null, null, null);

            var role = user.UserRoles.FirstOrDefault()?.Role?.Name;
            return (user.FirstName, user.LastName, user.Email, role);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve user info for user {UserId}", userId);
            return (null, null, null, null);
        }
    }

    // Helper method to convert UserChatMessage history to LlmChatMessage format
    private List<LlmChatMessage> ConvertToLlmChatMessages(IEnumerable<UserChatMessage> messages)
    {
        return ChatHelper.ConvertToLlmChatMessages(messages);
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

    public async Task<MultilingualChatResponse> SendUserMultilingualMessageAsync(string userId, string sessionId, Models.MultilingualChatRequest request, CancellationToken cancellationToken = default)
    {
        // Get user information for system message personalization
        var (firstName, lastName, email, role) = await GetUserInfoAsync(userId, cancellationToken);

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

        // Detect language if not provided, but prefer UI language
        var detectedLanguage = string.IsNullOrEmpty(request.Language)
            ? _languageService.DetectLanguage(request.Message)
            : request.Language;

        // Extract UI language from metadata if available
        var uiLanguage = request.Metadata?.TryGetValue("uiLanguage", out var uiLangObj) == true
            ? uiLangObj?.ToString()
            : null;

        // Priority: ResponseLanguage from UI > Language from UI > UI Language from metadata > detected > default
        var responseLanguage = request.ResponseLanguage ?? request.Language ?? uiLanguage ?? detectedLanguage ?? _languageService.GetDefaultLanguage();
        var normalizedResponseLanguage = _languageService.NormalizeLanguage(responseLanguage);

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
                m.Metadata,
                m.OllamaContext
            ))
            .ToListAsync(cancellationToken);

        // Add user message to database
        var userDbMessage = new ChatMessage
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
            null, // Sources
            userDbMessage.Metadata,
            null  // OllamaContext
        );
        conversationHistory.Add(userMessage);

        try
        {
            // Search for relevant context only if document search is enabled
            SearchResponse searchResults;
            if (request.UseDocumentSearch)
            {
                searchResults = await _searchService.SearchAsync(new SearchRequest(
                    request.Message,
                    Filters: null,
                    Limit: 1, // Get only one document with the highest rating
                    Offset: 0
                ), cancellationToken);
            }
            else
            {
                // Empty search results when document search is disabled
                searchResults = new SearchResponse(Array.Empty<SearchResult>(), 0, 0, request.Message);
            }

            // Debug logging for multilingual search results content
            _logger.LogDebug("Multilingual search results for prompt: {ResultCount} results", searchResults.Results.Length);
            foreach (var result in searchResults.Results)
            {
                var displayName = !string.IsNullOrEmpty(result.FileName) ? result.FileName : result.Source;
                _logger.LogDebug("Multilingual search result - Source: {Source}, FileName: {FileName}, Content length: {ContentLength}, Content preview: {ContentPreview}",
                    result.Source, displayName, result.Content?.Length ?? 0,
                    result.Content?.Length > 100 ? result.Content[..100] + "..." : result.Content ?? "NULL");
            }

            // Log sources being used in multilingual context
            if (searchResults.Results.Length > 0 && request.UseDocumentSearch)
            {
                var sources = searchResults.Results.Select(r => !string.IsNullOrEmpty(r.FileName) ? r.FileName : r.Source).Distinct().ToArray();
                _logger.LogInformation("Using documents as sources for multilingual response: {Sources}", string.Join(", ", sources));
            }

            // Check if Ollama is configured, if so use LLM service with context support
            string aiResponseContent;
            int[]? newOllamaContext = null;

            var llmSettings = await _globalSettingsService.GetLlmSettingsAsync();
            if (llmSettings != null && llmSettings.IsOllama)
            {
                // Inject documents into user message if document search is enabled and results found
                string enhancedUserMessage = request.Message;
                if (request.UseDocumentSearch && searchResults.Results.Length > 0)
                {
                    var documentsContext = BuildDocumentsContext(searchResults.Results, normalizedResponseLanguage);
                    enhancedUserMessage = $"{documentsContext}\n\n{request.Message}";

                    _logger.LogDebug("Enhanced multilingual user message with {DocumentCount} documents, total length: {MessageLength}",
                        searchResults.Results.Length, enhancedUserMessage.Length);
                }
                else if (!request.UseDocumentSearch)
                {
                    enhancedUserMessage = BuildMultilingualContextualPrompt(request.Message, normalizedResponseLanguage);
                    _logger.LogWarning("No documents found for multilingual user message, but document search was requested");
                }

                // Extract sources from conversation history and add to prompt
                var conversationSources = ExtractConversationSources(conversationHistory);
                if (conversationSources.Length > 0)
                {
                    var sourcesContext = BuildConversationSourcesContext(conversationSources);
                    enhancedUserMessage = $"{sourcesContext}\n\n{enhancedUserMessage}";
                    
                    _logger.LogDebug("Added {SourceCount} conversation sources to prompt", conversationSources.Length);
                }


                // Build message history (system message will be added by ChatService if needed)
                var messageHistory = ConvertToLlmChatMessages(conversationHistory.SkipLast(1)); // Exclude the just-added user message

                // Use new Chat API with system message handled by ChatService
                aiResponseContent = await _llmService.ChatWithHistoryAsync(
                    messageHistory,
                    enhancedUserMessage,
                    normalizedResponseLanguage, // Let ChatService handle system message
                    firstName,
                    lastName,
                    email,
                    role,
                    cancellationToken);

                _logger.LogDebug("Generated multilingual user response using Chat API with {HistoryCount} previous messages", messageHistory.Count());

                // Note: /api/chat doesn't return context tokens, so we can't preserve Ollama context
                newOllamaContext = null;
            }
            else
            {
                // Fallback to Semantic Kernel for non-Ollama providers
                var fallbackPrompt = BuildMultilingualContextualPrompt(
                    request.Message,
                    detectedLanguage = detectedLanguage ?? "en"
                );

                _logger.LogDebug("Final multilingual fallback prompt length: {PromptLength} characters", fallbackPrompt.Length);

                var aiResponse = await _kernel.InvokePromptAsync(fallbackPrompt, cancellationToken: cancellationToken);
                aiResponseContent = aiResponse.GetValue<string>() ??
                    _languageService.GetLocalizedErrorMessage("generation_failed", normalizedResponseLanguage);
            }

            // Save AI response to database
            var aiDbMessage = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = sessionId,
                Role = "assistant",
                Content = aiResponseContent,
                Timestamp = DateTime.UtcNow,
                Sources = searchResults.Results.Length > 0 && request.UseDocumentSearch ? searchResults.Results : null,
                Metadata = new Dictionary<string, object>
                {
                    ["responseLanguage"] = normalizedResponseLanguage,
                    ["documentsUsed"] = searchResults.Results.Length,
                    ["useDocumentSearch"] = request.UseDocumentSearch,
                    ["sourcesUsed"] = request.UseDocumentSearch && searchResults.Results.Length > 0
                        ? searchResults.Results.Select(r => !string.IsNullOrEmpty(r.FileName)
                            ? r.FileName
                            : !string.IsNullOrEmpty(r.FilePath)
                                ? Path.GetFileName(r.FilePath)
                                : r.Source ?? "Unknown").Distinct().ToArray()
                        : new string[0]
                },
                OllamaContext = newOllamaContext  // Save Ollama context for future token cache usage
            };

            _chatDbContext.ChatMessages.Add(aiDbMessage);

            // Update session timestamp
            dbSession.UpdatedAt = DateTime.UtcNow;

            await _chatDbContext.SaveChangesAsync(cancellationToken);

            return new MultilingualChatResponse
            {
                Response = aiDbMessage.Content,
                SessionId = sessionId,
                DetectedLanguage = detectedLanguage ?? "unknown",
                ResponseLanguage = normalizedResponseLanguage,
                WasTranslated = false, // TODO: Implement translation logic when needed
                Sources = searchResults.Results.Length > 0 && request.UseDocumentSearch ? searchResults.Results.Select(r => r.Content).ToList() : null,
                ProcessingTimeMs = 0, // TODO: Add timing measurement if needed
                Metadata = new Dictionary<string, object>
                {
                    ["useDocumentSearch"] = request.UseDocumentSearch,
                    ["documentsUsed"] = request.UseDocumentSearch ? searchResults.Results.Length : 0
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating multilingual AI response for user {UserId} session {SessionId}", userId, sessionId);

            // Save error message to database
            var errorDbMessage = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = sessionId,
                Role = "assistant",
                Content = _languageService.GetLocalizedErrorMessage("processing_error", normalizedResponseLanguage),
                Timestamp = DateTime.UtcNow
            };

            _chatDbContext.ChatMessages.Add(errorDbMessage);
            await _chatDbContext.SaveChangesAsync(cancellationToken);

            return new MultilingualChatResponse
            {
                Response = errorDbMessage.Content,
                SessionId = sessionId,
                DetectedLanguage = detectedLanguage ?? "unknown",
                ResponseLanguage = normalizedResponseLanguage,
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

    private string BuildMultilingualContextualPrompt(
        string userMessage,
        string responseLanguage)
    {
        var promptBuilder = new StringBuilder();

        // CRITICAL: Strong language instruction at the beginning
        var languageInstruction = _languageService.GetLocalizedString("instructions", "respond_in_language", responseLanguage);
        promptBuilder.AppendLine($"IMPORTANT: {languageInstruction}");
        promptBuilder.AppendLine($"MUST RESPOND IN: {responseLanguage.ToUpper()}");
        promptBuilder.AppendLine();

        // Add system instruction with language information using localization based on document search setting
        var systemPrompt = _languageService.GetLocalizedString("system_prompts", "rag_assistant_no_docs", responseLanguage);

        var contextInstruction = _languageService.GetLocalizedString("system_prompts", "context_instruction_no_docs", responseLanguage);

        promptBuilder.AppendLine(systemPrompt);
        promptBuilder.AppendLine(contextInstruction);


        promptBuilder.AppendLine();
        var noSearchNote = _languageService.GetLocalizedString("system_prompts", "no_document_search_note", responseLanguage)
            ?? "Note: Document search is disabled for this conversation.";
        promptBuilder.AppendLine($"=== UWAGA ===");
        promptBuilder.AppendLine(noSearchNote);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(_languageService.GetLocalizedString("instructions", "be_honest_no_docs", responseLanguage));

        promptBuilder.AppendLine();
        promptBuilder.AppendLine(_languageService.GetLocalizedString("instructions", "be_honest", responseLanguage));

        promptBuilder.AppendLine();
        promptBuilder.AppendLine(_languageService.GetLocalizedString("system_prompts", "current_question", responseLanguage));
        var userLabel = _languageService.GetLocalizedString("ui_labels", "user", responseLanguage);
        promptBuilder.AppendLine($"{userLabel} ({responseLanguage}): {userMessage}");
        promptBuilder.AppendLine();

        // FINAL CRITICAL REMINDER before response
        promptBuilder.AppendLine($"CRITICAL: {languageInstruction}");
        promptBuilder.AppendLine(_languageService.GetLocalizedString("system_prompts", "response", responseLanguage));

        return promptBuilder.ToString();
    }

    /// <summary>
    /// Formats document source information for inclusion in prompts
    /// <summary>
    /// Builds a prompt for chat API from localized JSON files
    /// </summary>
    private async Task<string> BuildChatPromptAsync(
        string userMessage,
        SearchResult[] searchResults,
        List<UserChatMessage> conversationHistory,
        string language,
        bool useDocumentSearch)
    {
        var promptBuilder = new StringBuilder();

        // Get system message from localized JSON file
        var systemMessage = await _llmService.GetSystemMessageAsync(language);
        promptBuilder.AppendLine(systemMessage);
        promptBuilder.AppendLine();

        // Add context instruction based on document search setting
        var contextInstruction = useDocumentSearch
            ? _languageService.GetLocalizedString("system_prompts", "context_instruction", language)
            : _languageService.GetLocalizedString("system_prompts", "context_instruction_no_docs", language);
        promptBuilder.AppendLine(contextInstruction);

        // Add context from search results if available and enabled
        if (useDocumentSearch && searchResults.Length > 0)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(_languageService.GetLocalizedString("system_prompts", "knowledge_base_context", language));

            // Add each document with its source information
            foreach (var result in searchResults)
            {
                promptBuilder.AppendLine();
                promptBuilder.AppendLine(ChatHelper.FormatDocumentSource(result, _languageService, language));
                promptBuilder.AppendLine($"- {result.Content}");
            }

            // Add summary of sources used
            if (searchResults.Length > 1)
            {
                promptBuilder.AppendLine();
                promptBuilder.Append(ChatHelper.FormatSourcesSummary(searchResults, _languageService, language));
            }
        }
        else if (!useDocumentSearch)
        {
            promptBuilder.AppendLine();
            var noSearchNote = _languageService.GetLocalizedString("system_prompts", "no_document_search_note", language)
                ?? "Note: Document search is disabled for this conversation.";
            promptBuilder.AppendLine($"=== UWAGA ===");
            promptBuilder.AppendLine(noSearchNote);
        }

        // Add recent conversation history (last 5 messages)
        var recentMessages = conversationHistory.TakeLast(5).ToArray();
        if (recentMessages.Length > 1) // More than just the current message
        {
            promptBuilder.AppendLine();
            //promptBuilder.AppendLine(_languageService.GetLocalizedString("system_prompts", "conversation_history", language));
            foreach (var msg in recentMessages.SkipLast(1)) // Skip the current message we're responding to
            {
                var roleLabel = msg.Role == "user"
                    ? _languageService.GetLocalizedString("ui_labels", "user", language)
                    : _languageService.GetLocalizedString("ui_labels", "assistant", language);
                promptBuilder.AppendLine($"{roleLabel}: {msg.Content}");
            }
        }

        // Add current user message
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(_languageService.GetLocalizedString("system_prompts", "current_question", language));
        var userLabel = _languageService.GetLocalizedString("ui_labels", "user", language);
        promptBuilder.AppendLine($"{userLabel}: {userMessage}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(_languageService.GetLocalizedString("system_prompts", "response", language));

        return promptBuilder.ToString();
    }

    /// <summary>
    /// Builds a multilingual prompt for chat API using from localized JSON files
    /// </summary>
    private async Task<string> BuildMultilingualChatPromptAsync(
        string userMessage,
        SearchResult[] searchResults,
        List<UserChatMessage> conversationHistory,
        string detectedLanguage,
        string responseLanguage,
        bool useDocumentSearch,
        bool documentsAvailable)
    {
        var promptBuilder = new StringBuilder();

        // CRITICAL: Strong language instruction at the beginning
        var languageInstruction = _languageService.GetLocalizedString("instructions", "respond_in_language", responseLanguage);
        promptBuilder.AppendLine($"IMPORTANT: {languageInstruction}");
        promptBuilder.AppendLine($"MUST RESPOND IN: {responseLanguage.ToUpper()}");
        promptBuilder.AppendLine();

        // Get system message from localized JSON file
        var systemMessage = await _llmService.GetSystemMessageAsync(responseLanguage);
        promptBuilder.AppendLine(systemMessage);
        promptBuilder.AppendLine();

        // Add context instruction based on document search setting
        var contextInstruction = useDocumentSearch
            ? _languageService.GetLocalizedString("system_prompts", "context_instruction", responseLanguage)
            : _languageService.GetLocalizedString("system_prompts", "context_instruction_no_docs", responseLanguage);
        promptBuilder.AppendLine(contextInstruction);

        if (useDocumentSearch && documentsAvailable && searchResults.Length > 0)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(_languageService.GetLocalizedString("system_prompts", "knowledge_base_context", responseLanguage));

            // Add each document with its source information
            foreach (var result in searchResults)
            {
                promptBuilder.AppendLine();
                promptBuilder.AppendLine(ChatHelper.FormatDocumentSource(result, _languageService, responseLanguage));
                promptBuilder.AppendLine($"- {result.Content}");
            }

            // Add summary of sources used
            if (searchResults.Length > 1)
            {
                promptBuilder.AppendLine();
                promptBuilder.Append(ChatHelper.FormatSourcesSummary(searchResults, _languageService, responseLanguage));
            }

            // Reinforce language instruction after context
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"REMINDER: {languageInstruction}");
        }
        else if (!useDocumentSearch)
        {
            promptBuilder.AppendLine();
            var noSearchNote = _languageService.GetLocalizedString("system_prompts", "no_document_search_note", responseLanguage)
                ?? "Note: Document search is disabled for this conversation.";
            promptBuilder.AppendLine($"=== UWAGA ===");
            promptBuilder.AppendLine(noSearchNote);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(_languageService.GetLocalizedString("instructions", "be_honest_no_docs", responseLanguage));
        }
        else
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(_languageService.GetLocalizedString("instructions", "be_honest", responseLanguage));
        }

        // Add recent conversation history
        var recentMessages = conversationHistory.TakeLast(5).ToArray();
        if (recentMessages.Length > 1)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(_languageService.GetLocalizedString("system_prompts", "conversation_history", responseLanguage));
            foreach (var msg in recentMessages.SkipLast(1))
            {
                var roleLabel = msg.Role == "user"
                    ? _languageService.GetLocalizedString("ui_labels", "user", responseLanguage)
                    : _languageService.GetLocalizedString("ui_labels", "assistant", responseLanguage);
                promptBuilder.AppendLine($"{roleLabel}: {msg.Content}");
            }
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine(_languageService.GetLocalizedString("system_prompts", "current_question", responseLanguage));
        var userLabel = _languageService.GetLocalizedString("ui_labels", "user", responseLanguage);
        promptBuilder.AppendLine($"{userLabel} ({detectedLanguage}): {userMessage}");
        promptBuilder.AppendLine();

        // FINAL CRITICAL REMINDER before response
        promptBuilder.AppendLine($"CRITICAL: {languageInstruction}");
        promptBuilder.AppendLine(_languageService.GetLocalizedString("system_prompts", "response", responseLanguage));

        return promptBuilder.ToString();
    }

    /// <summary>
    /// Build documents context for injection into user message with multilingual support
    /// </summary>
    private string BuildDocumentsContext(SearchResult[] searchResults, string language = "en")
    {
        if (searchResults.Length == 0)
            return string.Empty;

        var contextBuilder = new StringBuilder();
        var prePrompt = _languageService.GetLocalizedString("system_prompts", "rag_assistant", language)
            ?? "";
        if (!string.IsNullOrEmpty(prePrompt))
        {
            contextBuilder.AppendLine(prePrompt);
            contextBuilder.AppendLine();
        }
        var languageInstruction = _languageService.GetLocalizedString("instructions", "respond_in_language", language);
        contextBuilder.AppendLine($"IMPORTANT: {languageInstruction}");
        contextBuilder.AppendLine($"MUST RESPOND IN: {language.ToUpper()}");

        // Add context header using localization
        var contextHeader = _languageService.GetLocalizedString("system_prompts", "knowledge_base_context", language)
            ?? "=== KNOWLEDGE BASE CONTEXT ===";
        contextBuilder.AppendLine(contextHeader);
        contextBuilder.AppendLine();

        foreach (var result in searchResults)
        {
            // Use FormatDocumentSource for consistent formatting
            contextBuilder.AppendLine(ChatHelper.FormatDocumentSource(result, _languageService, language));

            // Add document type and file path information
            var documentLabel = _languageService.GetLocalizedString("ui_labels", "document", language) ?? "Document";
            var typeLabel = _languageService.GetLocalizedString("ui_labels", "type", language) ?? "Type";
            var pathLabel = _languageService.GetLocalizedString("ui_labels", "path", language) ?? "Path";

            contextBuilder.AppendLine($"{documentLabel}: {result.DocumentType}");
            if (!string.IsNullOrEmpty(result.FilePath))
            {
                contextBuilder.AppendLine($"{pathLabel}: {result.FilePath}");
            }

            // Add score information if available
            if (result.Metadata.TryGetValue("score", out var scoreObj) && scoreObj is double score)
            {
                var scoreLabel = _languageService.GetLocalizedString("ui_labels", "score", language) ?? "Score";
                contextBuilder.AppendLine($"{scoreLabel}: {score:F2}");
            }

            // Add content with highlights if available
            if (result.Metadata.TryGetValue("highlights", out var highlightsObj) && highlightsObj is string highlights)
            {
                contextBuilder.AppendLine($"{_languageService.GetLocalizedString("ui_labels", "highlights", language) ?? "Highlights"}:");
                contextBuilder.AppendLine($"- {highlights}");
                contextBuilder.AppendLine($"{_languageService.GetLocalizedString("ui_labels", "full_content", language) ?? "Full Content"}:");
                contextBuilder.AppendLine($"- {result.Content}");
            }
            else
            {
                contextBuilder.AppendLine($"- {result.Content}");
            }

            // Add reconstruction info if applicable
            if (result.Metadata.TryGetValue("reconstructed", out var reconstructedObj) &&
                reconstructedObj is bool reconstructed && reconstructed)
            {
                if (result.Metadata.TryGetValue("chunksFound", out var chunksFoundObj) &&
                    result.Metadata.TryGetValue("totalChunks", out var totalChunksObj))
                {
                    var chunksFound = chunksFoundObj is int cf ? cf : 0;
                    var totalChunks = totalChunksObj is int tc ? tc : 0;
                    var reconstructionNote = _languageService.GetLocalizedString("system_prompts", "reconstructed_from_chunks", language);
                    if (!string.IsNullOrEmpty(reconstructionNote))
                    {
                        contextBuilder.AppendLine(string.Format(reconstructionNote, totalChunks));
                    }
                }
            }

            contextBuilder.AppendLine();
        }

        // Add summary of sources used if multiple
        if (searchResults.Length > 1)
        {
            contextBuilder.Append(ChatHelper.FormatSourcesSummary(searchResults, _languageService, language));
            contextBuilder.AppendLine();

            // Add honesty instruction
            var beHonestInstruction = _languageService.GetLocalizedString("instructions", "be_honest", language);
            if (!string.IsNullOrEmpty(beHonestInstruction))
            {
                contextBuilder.AppendLine($"REMINDER: {beHonestInstruction}");
            }
        }

        // Add context footer using localization
        var contextFooter = _languageService.GetLocalizedString("system_prompts", "document_source_intro", language)
            ?? "=== END OF KNOWLEDGE BASE CONTEXT ===";
        contextBuilder.AppendLine(contextFooter);
        contextBuilder.AppendLine($"CRITICAL: {languageInstruction}");
        contextBuilder.AppendLine(_languageService.GetLocalizedString("system_prompts", "response", language));

        return contextBuilder.ToString();
    }

    /// <summary>
    /// Extract distinct source names from conversation history
    /// </summary>
    private string[] ExtractConversationSources(List<UserChatMessage> conversationHistory)
    {
        var sources = new HashSet<string>();
        
        foreach (var message in conversationHistory)
        {
            if (message.Sources != null)
            {
                foreach (var result in message.Sources)
                {
                    var sourceName = !string.IsNullOrEmpty(result.FileName) 
                        ? result.FileName 
                        : !string.IsNullOrEmpty(result.FilePath)
                            ? Path.GetFileName(result.FilePath)
                            : result.Source ?? "Unknown";
                    
                    if (!string.IsNullOrEmpty(sourceName))
                    {
                        sources.Add(sourceName);
                    }
                }
            }
        }
        
        return sources.ToArray();
    }

    /// <summary>
    /// Build conversation sources context for injection into prompt
    /// </summary>
    private string BuildConversationSourcesContext(string[] sources)
    {
        if (sources.Length == 0)
            return string.Empty;

        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("sources used in this conversation");
        contextBuilder.AppendLine();
        
        foreach (var source in sources)
        {
            contextBuilder.AppendLine($"- {source}");
        }
        
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("end of sources used in this conversation");
        contextBuilder.AppendLine();
        
        return contextBuilder.ToString();
    }
}
