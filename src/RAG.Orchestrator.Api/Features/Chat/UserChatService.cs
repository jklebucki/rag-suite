using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using RAG.Abstractions.Search;
using RAG.DocumentProcessing.Abstractions;
using RAG.Orchestrator.Api.Common.Constants;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Features.Chat.Attachments;
using RAG.Orchestrator.Api.Features.Chat.Artifacts;
using RAG.Orchestrator.Api.Features.Chat.Documents;
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
    private readonly IChatAttachmentService _chatAttachmentService;
    private readonly IGeneratedArtifactService _generatedArtifactService;
    private readonly IArtifactSessionCleanupService _artifactSessionCleanupService;

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
        IPromptBuilder promptBuilder,
        IChatAttachmentService chatAttachmentService,
        IGeneratedArtifactService generatedArtifactService,
        IArtifactSessionCleanupService artifactSessionCleanupService)
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
        _chatAttachmentService = chatAttachmentService;
        _generatedArtifactService = generatedArtifactService;
        _artifactSessionCleanupService = artifactSessionCleanupService;
    }

    private async Task<LlmUserContext?> GetUserInfoAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _securityDbContext.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
                return null;

            var roles = user.UserRoles
                .Select(userRole => userRole.Role?.Name)
                .OfType<string>()
                .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
                .Distinct()
                .OrderBy(roleName => roleName)
                .ToArray();

            return new LlmUserContext
            {
                UserId = user.Id,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Roles = roles
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve user info for user {UserId}", userId);
            return null;
        }
    }

    private static List<LlmChatMessage> ConvertToLlmChatMessages(
        IEnumerable<UserChatMessage> messages,
        ILookup<string, ChatDocument> documentsByUserMessage)
    {
        return messages
            .Where(message => message.Role is ChatRoles.User or ChatRoles.Assistant)
            .Select(message => new LlmChatMessage
            {
                Role = message.Role,
                Content = message.Role == ChatRoles.User
                    ? ChatDocumentMarkdown.AppendToMessage(message.Content, documentsByUserMessage[message.Id])
                    : message.Content
            })
            .ToList();
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
        var userContext = await GetUserInfoAsync(userId, cancellationToken);

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

        var preparedAttachments = await _chatAttachmentService.PrepareForMessageAsync(
            userId,
            sessionId,
            request.Message,
            request.AttachmentIds,
            cancellationToken);

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

        var persistedDocuments = await _chatDbContext.ChatDocuments
            .AsNoTracking()
            .Where(document => document.UserMessage.SessionId == sessionId)
            .OrderBy(document => document.CreatedAt)
            .ToListAsync(cancellationToken);

        var userMetadata = new Dictionary<string, object>
        {
            ["detectedLanguage"] = detectedLanguage ?? "unknown",
            ["originalLanguage"] = detectedLanguage ?? "unknown"
        };

        var userMessageId = Guid.NewGuid().ToString();
        var currentDocuments = preparedAttachments.Files
            .Where(file => !string.IsNullOrWhiteSpace(file.Content))
            .Select(file => new ChatDocument
            {
                Id = Guid.NewGuid().ToString(),
                UserMessageId = userMessageId,
                FileName = file.FileName,
                ContentType = file.ContentType,
                Markdown = file.Content,
                SizeBytes = file.SizeBytes,
                TokenCount = file.TokenCount,
                PageCount = file.PageCount,
                Provider = file.Provider,
                CreatedAt = DateTime.UtcNow
            })
            .ToArray();

        if (preparedAttachments.Files.Length > 0)
        {
            userMetadata["attachments"] = preparedAttachments.Files
                .Select(file => new
                {
                    file.Id,
                    file.FileName,
                    file.ContentType,
                    file.SizeBytes,
                    file.TokenCount
                })
                .ToArray();
            userMetadata["attachmentsTokenCount"] = preparedAttachments.TokenCount;
        }

        if (currentDocuments.Length > 0)
        {
            userMetadata["ocrDocuments"] = BuildDocumentMetadata(currentDocuments);
        }

        // Add user message to database
        var userDbMessage = new ChatMessage
        {
            Id = userMessageId,
            SessionId = sessionId,
            Role = ChatRoles.User,
            Content = request.Message,
            Timestamp = DateTime.UtcNow,
            Metadata = userMetadata
        };

        _chatDbContext.ChatMessages.Add(userDbMessage);
        _chatDbContext.ChatDocuments.AddRange(currentDocuments);
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

        // First exchange in the session (only the just-added user message) — used for the title fallback.
        var isFirstExchange = conversationHistory.Count == 1;

        var documentsByUserMessage = persistedDocuments.ToLookup(document => document.UserMessageId);
        var requestedExportFormat = ChatDocumentRequestClassifier.GetRequestedExportFormat(request.Message);
        var requestsDirectCanonicalResponse = ChatDocumentRequestClassifier.RequestsDirectCanonicalResponse(request.Message);
        var directResponseDocuments = currentDocuments.Length > 0 ? currentDocuments : persistedDocuments.ToArray();
        var createsLlmDocumentArtifact = directResponseDocuments.Length > 0 &&
                                         requestedExportFormat != null &&
                                         !requestsDirectCanonicalResponse;
        var userMessageForLlm = createsLlmDocumentArtifact
            ? BuildDocumentTransformationMessage(
                request.Message,
                requestedExportFormat!.Value,
                currentDocuments.Length == 0 ? directResponseDocuments : Array.Empty<ChatDocument>())
            : request.Message;

        var llmSettings = await _globalSettingsService.GetLlmSettingsAsync();

        try
        {
            if (directResponseDocuments.Length > 0 && requestsDirectCanonicalResponse)
            {
                return await CreateCanonicalDocumentResponseAsync(
                    directResponseDocuments,
                    requestedExportFormat,
                    userId,
                    sessionId,
                    userDbMessage,
                    dbSession,
                    request,
                    normalizedResponseLanguage,
                    detectedLanguage,
                    isFirstExchange,
                    preparedAttachments,
                    cancellationToken);
            }

            // Search for relevant context only if document search is enabled
            SearchResponse searchResults;
            if (request.UseDocumentSearch)
            {
                // Retrieve the top-N documents so the relevant one is included even when a
                // marginally-related document (e.g. keyword-stuffed) outranks it as the single top hit.
                // The limit is configurable from the LLM settings in the UI.
                var documentSearchLimit = Math.Max(1, llmSettings?.DocumentSearchLimit ?? 4);
                searchResults = await _searchService.SearchAsync(new SearchRequest(
                    request.Message,
                    Filters: null,
                    Limit: documentSearchLimit,
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

            var attachmentsContext = ChatAttachmentService.BuildAttachmentsPromptBlock(preparedAttachments.Files);
            if (llmSettings != null && llmSettings.IsOllama)
            {
                // Inject documents into user message if document search is enabled and results found
                string enhancedUserMessage = userMessageForLlm;
                if (request.UseDocumentSearch && searchResults.Results.Length > 0)
                {
                    var documentsContext = _promptBuilder.BuildDocumentsContext(searchResults.Results, normalizedResponseLanguage);
                    enhancedUserMessage = $"{documentsContext}\n\n{userMessageForLlm}";

                    _logger.LogDebug("Enhanced multilingual user message with {DocumentCount} documents, total length: {MessageLength}",
                        searchResults.Results.Length, enhancedUserMessage.Length);
                }
                else if (!request.UseDocumentSearch)
                {
                    var promptContext = new PromptContext
                    {
                        UserMessage = userMessageForLlm,
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

                if (!string.IsNullOrWhiteSpace(attachmentsContext))
                {
                    enhancedUserMessage = $"{attachmentsContext}\n\n{enhancedUserMessage}";
                    _logger.LogDebug("Enhanced multilingual user message with {AttachmentCount} temporary attachments, attachment tokens: {AttachmentTokens}",
                        preparedAttachments.Files.Length, preparedAttachments.TokenCount);
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
                var messageHistory = ConvertToLlmChatMessages(conversationHistory.SkipLast(1), documentsByUserMessage); // Exclude the just-added user message

                // Use new Chat API with system message handled by ChatService
                aiResponseContent = await _llmService.ChatWithHistoryAsync(
                    messageHistory,
                    enhancedUserMessage,
                    normalizedResponseLanguage, // Let ChatService handle system message
                    userContext,
                    cancellationToken);

                _logger.LogDebug("Generated multilingual user response using Chat API with {HistoryCount} previous messages", messageHistory.Count());

                // Note: /api/chat doesn't return context tokens, so we can't preserve Ollama context
                newOllamaContext = null;
            }
            else
            {
                var userMessageForPrompt = !string.IsNullOrWhiteSpace(attachmentsContext)
                    ? $"{attachmentsContext}\n\n{userMessageForLlm}"
                    : userMessageForLlm;

                // Fallback to Semantic Kernel for non-Ollama providers
                var promptContext = new PromptContext
                {
                    UserMessage = userMessageForPrompt,
                    SearchResults = searchResults.Results,
                    ConversationHistory = conversationHistory.Select(m => new MessageContext
                    {
                        Role = m.Role,
                        Content = m.Role == ChatRoles.User
                            ? ChatDocumentMarkdown.AppendToMessage(m.Content, documentsByUserMessage[m.Id])
                            : m.Content
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

            // Extract the LLM-provided conversation title (CHAT_TITLE marker) and strip it from the answer.
            var (cleanedResponse, extractedTitle) = ChatTitleExtractor.Extract(aiResponseContent);

            // Use cleaned response (without the title marker line) for saving
            aiResponseContent = cleanedResponse;
            var assistantMessageId = Guid.NewGuid().ToString();
            ArtifactGenerationResult artifactResult;
            if (createsLlmDocumentArtifact)
            {
                var transformedDocumentMarkdown = ExtractTransformedDocumentMarkdown(aiResponseContent);
                artifactResult = await _generatedArtifactService.CreateAsync(
                    requestedExportFormat!.Value,
                    ChatDocumentMarkdown.GetExportFileName(directResponseDocuments, requestedExportFormat.Value),
                    transformedDocumentMarkdown,
                    userId,
                    sessionId,
                    assistantMessageId,
                    cancellationToken);
                aiResponseContent = transformedDocumentMarkdown;
                if (artifactResult.ArtifactCreated)
                {
                    aiResponseContent = $"{aiResponseContent.Trim()}\n\n{artifactResult.Response}";
                }
            }
            else
            {
                artifactResult = await _generatedArtifactService.ProcessAsync(
                    aiResponseContent,
                    userId,
                    sessionId,
                    assistantMessageId,
                    cancellationToken);
                aiResponseContent = artifactResult.Response;
            }

            if (!string.IsNullOrWhiteSpace(extractedTitle))
            {
                dbSession.Title = extractedTitle;
                _logger.LogInformation("Updated session {SessionId} title from LLM marker: {Title}", sessionId, extractedTitle);
            }
            else if (isFirstExchange)
            {
                // Fallback: the model omitted the marker — derive a title from the user's first message so
                // the session never keeps the default "new conversation" placeholder.
                var fallbackTitle = ChatTitleExtractor.BuildFallbackTitle(request.Message);
                if (!string.IsNullOrWhiteSpace(fallbackTitle))
                {
                    dbSession.Title = fallbackTitle;
                    _logger.LogInformation("Set session {SessionId} title from first user message (no LLM marker): {Title}", sessionId, fallbackTitle);
                }
            }

            // Save AI response to database
            var aiDbMessage = new ChatMessage
            {
                Id = assistantMessageId,
                SessionId = sessionId,
                Role = ChatRoles.Assistant,
                Content = aiResponseContent,
                Timestamp = DateTime.UtcNow,
                Sources = searchResults.Results.Length > 0 && request.UseDocumentSearch ? searchResults.Results : null,
                Metadata = CreateAssistantMetadata(
                    normalizedResponseLanguage,
                    searchResults.Results,
                    request.UseDocumentSearch,
                    artifactResult.ArtifactCreated,
                    currentDocuments),
                OllamaContext = newOllamaContext  // Save Ollama context for future token cache usage
            };

            _chatDbContext.ChatMessages.Add(aiDbMessage);

            // Update session timestamp
            dbSession.UpdatedAt = DateTime.UtcNow;

            await _chatDbContext.SaveChangesAsync(cancellationToken);
            await _chatAttachmentService.CommitMessageAttachmentsAsync(userId, sessionId, request.AttachmentIds, cancellationToken);
            var contextUsage = await _chatAttachmentService.GetContextAsync(userId, sessionId, cancellationToken);

            return new MultilingualChatResponse
            {
                Response = aiDbMessage.Content,
                SessionId = sessionId,
                UserMessageId = userDbMessage.Id,
                AssistantMessageId = aiDbMessage.Id,
                DetectedLanguage = detectedLanguage ?? "unknown",
                ResponseLanguage = normalizedResponseLanguage,
                WasTranslated = false, // TODO: Implement translation logic when needed
                Sources = searchResults.Results.Length > 0 && request.UseDocumentSearch ? searchResults.Results.Select(r => r.Content).ToList() : null,
                ProcessingTimeMs = 0, // TODO: Add timing measurement if needed
                Metadata = new Dictionary<string, object>
                {
                    ["useDocumentSearch"] = request.UseDocumentSearch,
                    ["documentsUsed"] = request.UseDocumentSearch ? searchResults.Results.Length : 0,
                    ["attachmentsUsed"] = preparedAttachments.Files.Length,
                    ["contextUsage"] = contextUsage ?? preparedAttachments.ContextUsage
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
            await _chatAttachmentService.CommitMessageAttachmentsAsync(userId, sessionId, request.AttachmentIds, cancellationToken);
            var contextUsage = await _chatAttachmentService.GetContextAsync(userId, sessionId, cancellationToken);

            return new MultilingualChatResponse
            {
                Response = errorDbMessage.Content,
                SessionId = sessionId,
                UserMessageId = userDbMessage.Id,
                AssistantMessageId = errorDbMessage.Id,
                DetectedLanguage = detectedLanguage ?? "unknown",
                ResponseLanguage = normalizedResponseLanguage,
                WasTranslated = false,
                Sources = null,
                ProcessingTimeMs = 0,
                Metadata = new Dictionary<string, object>
                {
                    ["attachmentsUsed"] = preparedAttachments.Files.Length,
                    ["contextUsage"] = contextUsage ?? preparedAttachments.ContextUsage
                }
            };
        }
    }

    private async Task<MultilingualChatResponse> CreateCanonicalDocumentResponseAsync(
        ChatDocument[] documents,
        GeneratedArtifactFormat? exportFormat,
        string userId,
        string sessionId,
        ChatMessage userDbMessage,
        ChatSession dbSession,
        MultilingualChatRequest request,
        string responseLanguage,
        string? detectedLanguage,
        bool isFirstExchange,
        PreparedChatAttachments preparedAttachments,
        CancellationToken cancellationToken)
    {
        var assistantMessageId = Guid.NewGuid().ToString();
        ArtifactGenerationResult artifactResult;
        string response;

        if (exportFormat == null)
        {
            response = ChatDocumentMarkdown.BuildDisplayResponse(documents);
            artifactResult = new ArtifactGenerationResult(response, false);
        }
        else
        {
            artifactResult = await _generatedArtifactService.CreateAsync(
                exportFormat.Value,
                ChatDocumentMarkdown.GetExportFileName(documents, exportFormat.Value),
                ChatDocumentMarkdown.BuildExportMarkdown(documents),
                userId,
                sessionId,
                assistantMessageId,
                cancellationToken);
            response = artifactResult.ArtifactCreated
                ? $"## Eksport OCR\n\n{artifactResult.Response}"
                : "Nie udało się przygotować pliku OCR. Wynik Markdown pozostaje dostępny w historii czatu.";
        }

        if (isFirstExchange)
        {
            var fallbackTitle = ChatTitleExtractor.BuildFallbackTitle(request.Message);
            if (!string.IsNullOrWhiteSpace(fallbackTitle))
            {
                dbSession.Title = fallbackTitle;
            }
        }

        var assistantMessage = new ChatMessage
        {
            Id = assistantMessageId,
            SessionId = sessionId,
            Role = ChatRoles.Assistant,
            Content = response,
            Timestamp = DateTime.UtcNow,
            Metadata = CreateAssistantMetadata(
                responseLanguage,
                Array.Empty<SearchResult>(),
                useDocumentSearch: false,
                artifactResult.ArtifactCreated,
                documents)
        };

        _chatDbContext.ChatMessages.Add(assistantMessage);
        dbSession.UpdatedAt = DateTime.UtcNow;
        await _chatDbContext.SaveChangesAsync(cancellationToken);
        await _chatAttachmentService.CommitMessageAttachmentsAsync(userId, sessionId, request.AttachmentIds, cancellationToken);
        var contextUsage = await _chatAttachmentService.GetContextAsync(userId, sessionId, cancellationToken);

        return new MultilingualChatResponse
        {
            Response = assistantMessage.Content,
            SessionId = sessionId,
            UserMessageId = userDbMessage.Id,
            AssistantMessageId = assistantMessage.Id,
            DetectedLanguage = detectedLanguage ?? "unknown",
            ResponseLanguage = responseLanguage,
            WasTranslated = false,
            Sources = null,
            ProcessingTimeMs = 0,
            Metadata = new Dictionary<string, object>
            {
                ["attachmentsUsed"] = preparedAttachments.Files.Length,
                ["generatedArtifact"] = artifactResult.ArtifactCreated,
                ["contextUsage"] = contextUsage ?? preparedAttachments.ContextUsage
            }
        };
    }

    private static Dictionary<string, object> CreateAssistantMetadata(
        string responseLanguage,
        SearchResult[] searchResults,
        bool useDocumentSearch,
        bool artifactCreated,
        IEnumerable<ChatDocument> documents)
    {
        var documentArray = documents.ToArray();
        var metadata = new Dictionary<string, object>
        {
            ["responseLanguage"] = responseLanguage,
            ["documentsUsed"] = searchResults.Length,
            ["useDocumentSearch"] = useDocumentSearch,
            ["generatedArtifact"] = artifactCreated,
            ["sourcesUsed"] = useDocumentSearch && searchResults.Length > 0
                ? searchResults.Select(result => !string.IsNullOrEmpty(result.FileName)
                    ? result.FileName
                    : !string.IsNullOrEmpty(result.FilePath)
                        ? Path.GetFileName(result.FilePath)
                        : result.Source ?? "Unknown").Distinct().ToArray()
                : Array.Empty<string>()
        };

        if (documentArray.Length > 0)
        {
            metadata["ocrDocuments"] = BuildDocumentMetadata(documentArray);
        }

        return metadata;
    }

    private static string BuildDocumentTransformationMessage(
        string message,
        GeneratedArtifactFormat format,
        IEnumerable<ChatDocument> documents)
    {
        var documentContext = ChatDocumentMarkdown.AppendToMessage(string.Empty, documents);
        var formatLabel = format == GeneratedArtifactFormat.Docx ? "DOCX" : "TXT";
        var instruction = $"""
            {message}

            === SERVER DOCUMENT OUTPUT INSTRUCTION ===
            Transform the canonical OCR document according to the user's request. Return only the complete transformed document in Markdown, without commentary, code fences, or generated_artifact tags. Preserve all relevant sections, headings, lists, and tables unless the user explicitly asks to change them. The server will save this exact Markdown as {formatLabel}.
            === END SERVER DOCUMENT OUTPUT INSTRUCTION ===
            """;

        return string.IsNullOrWhiteSpace(documentContext)
            ? instruction
            : $"{documentContext}\n\n{instruction}";
    }

    private static string ExtractTransformedDocumentMarkdown(string response)
    {
        try
        {
            var artifact = GeneratedArtifactBlockParser.Extract(response, out _);
            return artifact?.Markdown ?? response.Trim();
        }
        catch (DocumentProcessingException)
        {
            return response.Trim();
        }
    }

    private static object[] BuildDocumentMetadata(IEnumerable<ChatDocument> documents)
    {
        return documents.Select(document => (object)new
        {
            document.Id,
            document.FileName,
            document.ContentType,
            document.SizeBytes,
            document.PageCount,
            document.Provider
        }).ToArray();
    }

    public async Task<bool> DeleteUserSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
    {
        var deleted = await _sessionManager.DeleteUserSessionAsync(userId, sessionId, cancellationToken);
        if (deleted)
        {
            await _chatAttachmentService.ClearSessionAsync(userId, sessionId, cancellationToken);
            await _artifactSessionCleanupService.DeleteForSessionAsync(userId, sessionId, cancellationToken);
        }

        return deleted;
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

}
