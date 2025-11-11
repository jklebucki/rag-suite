using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using RAG.Abstractions.Search;
using RAG.Orchestrator.Api.Common.Constants;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Features.Chat.Prompting;
using RAG.Orchestrator.Api.Features.Chat.SessionManagement;
using RAG.Orchestrator.Api.Localization;
using RAG.Orchestrator.Api.Models;
using RAG.Orchestrator.Api.Services;
using RAG.Security.Data;
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
    private readonly ISessionManager _sessionManager;
    private readonly IPromptBuilder _promptBuilder;

    public UserChatService(
        ChatDbContext chatDbContext,
        SecurityDbContext securityDbContext,
        Kernel kernel,
        ISearchService searchService,
        ILanguageService languageService,
        ILogger<UserChatService> logger,
        IConfiguration configuration,
        ILlmService llmService,
        IGlobalSettingsService globalSettingsService,
        ISessionManager sessionManager,
        IPromptBuilder promptBuilder)
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
        _sessionManager = sessionManager;
        _promptBuilder = promptBuilder;
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


    public async Task<UserChatSession[]> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _sessionManager.GetUserSessionsAsync(userId, cancellationToken);
    }

    public async Task<UserChatSession> CreateUserSessionAsync(string userId, CreateUserSessionRequest request, CancellationToken cancellationToken = default)
    {
        return await _sessionManager.CreateUserSessionAsync(userId, request, cancellationToken);
    }

    public async Task<UserChatSession?> GetUserSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
    {
        return await _sessionManager.GetUserSessionAsync(userId, sessionId, cancellationToken);
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

        var maxMessageLength = _configuration.GetValue<int>(ConfigurationKeys.Chat.MaxMessageLength, 2000);
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
        var responseLanguage = request.ResponseLanguage ?? request.Language ?? uiLanguage ?? detectedLanguage ?? SupportedLanguages.Default;
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
            Role = ChatRoles.User,
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
                    var documentsContext = _promptBuilder.BuildDocumentsContext(searchResults.Results, normalizedResponseLanguage);
                    enhancedUserMessage = $"{documentsContext}\n\n{request.Message}";

                    _logger.LogDebug("Enhanced multilingual user message with {DocumentCount} documents, total length: {MessageLength}",
                        searchResults.Results.Length, enhancedUserMessage.Length);
                }
                else if (!request.UseDocumentSearch)
                {
                    var promptContext = new PromptContext
                    {
                        UserMessage = request.Message,
                        SearchResults = Array.Empty<SearchResult>(),
                        ConversationHistory = Array.Empty<MessageContext>(),
                        ResponseLanguage = normalizedResponseLanguage,
                        DetectedLanguage = detectedLanguage,
                        UseDocumentSearch = false,
                        DocumentsAvailable = false
                    };
                    enhancedUserMessage = _promptBuilder.BuildMultilingualContextualPrompt(promptContext);
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
                var promptContext = new PromptContext
                {
                    UserMessage = request.Message,
                    SearchResults = searchResults.Results,
                    ConversationHistory = conversationHistory.Select(m => new MessageContext
                    {
                        Role = m.Role,
                        Content = m.Content
                    }).ToList(),
                    ResponseLanguage = normalizedResponseLanguage,
                    DetectedLanguage = detectedLanguage ?? SupportedLanguages.English,
                    UseDocumentSearch = request.UseDocumentSearch,
                    DocumentsAvailable = searchResults.Results.Length > 0
                };
                var fallbackPrompt = _promptBuilder.BuildMultilingualContextualPrompt(promptContext);

                _logger.LogDebug("Final multilingual fallback prompt length: {PromptLength} characters", fallbackPrompt.Length);

                var aiResponse = await _kernel.InvokePromptAsync(fallbackPrompt, cancellationToken: cancellationToken);
                aiResponseContent = aiResponse.GetValue<string>() ??
                    _languageService.GetLocalizedErrorMessage("generation_failed", normalizedResponseLanguage);
            }

            // Extract summary from the LLM response and update session title if present
            var (cleanedResponse, extractedSummary) = ExtractSummaryFromResponse(aiResponseContent);

            // Use cleaned response (without summary line) for saving
            aiResponseContent = cleanedResponse;

            // Update session title if summary was found
            if (!string.IsNullOrWhiteSpace(extractedSummary))
            {
                dbSession.Title = extractedSummary;
                _logger.LogInformation("Updated session {SessionId} title from LLM summary: {Title}", sessionId, extractedSummary);
            }

            // Save AI response to database
            var aiDbMessage = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = sessionId,
                Role = ChatRoles.Assistant,
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
                Role = ChatRoles.Assistant,
                Content = _languageService.GetLocalizedErrorMessage(LocalizationKeys.ErrorMessages.ProcessingError, normalizedResponseLanguage),
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
        return await _sessionManager.DeleteUserSessionAsync(userId, sessionId, cancellationToken);
    }

    /// <summary>
    /// Build documents context for injection into user message with multilingual support
    /// NOTE: This method is now deprecated - use IPromptBuilder.BuildDocumentsContext instead
    /// </summary>
    [Obsolete("Use IPromptBuilder.BuildDocumentsContext instead")]
    private string BuildDocumentsContext(SearchResult[] searchResults, string language = SupportedLanguages.Default)
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

    /// <summary>
    /// Extracts summary from the last line of LLM response if it's enclosed in {} brackets.
    /// Returns the cleaned response and the extracted summary.
    /// </summary>
    /// <param name="response">The LLM response text</param>
    /// <returns>Tuple containing cleaned response and extracted summary (null if no summary found)</returns>
    private (string cleanedResponse, string? extractedSummary) ExtractSummaryFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return (response, null);

        var lines = response.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        if (lines.Length == 0)
            return (response, null);

        var lastLine = lines[^1].Trim();

        // Check if the last line contains a summary enclosed in {}
        var match = System.Text.RegularExpressions.Regex.Match(lastLine, @"^\{(.+)\}$");
        if (match.Success)
        {
            var summary = match.Groups[1].Value.Trim();

            // Remove the last line from the response
            var cleanedLines = lines.Take(lines.Length - 1).ToArray();
            var cleanedResponse = string.Join(Environment.NewLine, cleanedLines).TrimEnd();

            _logger.LogInformation("Extracted summary from LLM response: {Summary}", summary);

            return (cleanedResponse, summary);
        }

        return (response, null);
    }
}
