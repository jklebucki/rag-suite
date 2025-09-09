using RAG.Orchestrator.Api.Features.Chat;
using RAG.Abstractions.Search;
using RAG.Orchestrator.Api.Localization;
using RAG.Orchestrator.Api.Models;
using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;

namespace RAG.Orchestrator.Api.Features.Chat;

public interface IChatService
{
    Task<ChatSession[]> GetSessionsAsync(CancellationToken cancellationToken = default);
    Task<ChatSession> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<ChatMessage> SendMessageAsync(string sessionId, ChatRequest request, CancellationToken cancellationToken = default);
    Task<Models.MultilingualChatResponse> SendMultilingualMessageAsync(string sessionId, Models.MultilingualChatRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}

public interface ILlmService
{
    Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default);
    Task<(string response, int[]? context)> GenerateResponseWithContextAsync(string prompt, int[]? context = null, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    Task<string[]> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
}

public class LlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LlmService> _logger;

    public LlmService(HttpClient httpClient, IConfiguration configuration, ILogger<LlmService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var baseUrl = configuration["Services:LlmService:Url"] ?? "http://localhost:11434";
        _httpClient.BaseAddress = new Uri(baseUrl);

        // HttpClient timeout is now configured in ServiceCollectionExtensions for health operations
        // Chat operations use Semantic Kernel with different timeout configuration
    }

    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var (response, _) = await GenerateResponseWithContextAsync(prompt, null, cancellationToken);
        return response;
    }

    public async Task<(string response, int[]? context)> GenerateResponseWithContextAsync(string prompt, int[]? context = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var maxTokens = _configuration.GetValue<int>("Services:LlmService:MaxTokens", 200);
            var temperature = _configuration.GetValue<double>("Services:LlmService:Temperature", 0.7);
            var model = _configuration.GetValue<string>("Services:LlmService:Model", "qwen2:0.5b");
            var isOllama = _configuration.GetValue<bool>("Services:LlmService:IsOllama", false);

            object request;
            string endpoint;

            if (isOllama)
            {
                // Ollama API format with context support
                var requestObj = new
                {
                    model = model,
                    prompt = prompt,
                    stream = false,
                    options = new
                    {
                        temperature = temperature,
                        num_predict = maxTokens
                    }
                };

                // Add context if provided (for Ollama token cache)
                if (context != null && context.Length > 0)
                {
                    request = new
                    {
                        model = requestObj.model,
                        prompt = requestObj.prompt,
                        stream = requestObj.stream,
                        context = context, // Ollama context for token cache
                        options = requestObj.options
                    };
                }
                else
                {
                    request = requestObj;
                }
                
                endpoint = "/api/generate";
            }
            else
            {
                // TGI API format (no context support)
                var topP = _configuration.GetValue<double>("Services:LlmService:TopP", 0.9);
                request = new
                {
                    inputs = prompt,
                    parameters = new
                    {
                        max_new_tokens = maxTokens,
                        temperature = temperature,
                        top_p = topP,
                        do_sample = true,
                        return_full_text = false
                    }
                };
                endpoint = "/generate";
            }

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending request to LLM service with context: {Json}", json);

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("LLM service returned error: {StatusCode}, Content: {ErrorContent}", response.StatusCode, errorContent);
                return ("Sorry, there was a problem generating the response. Please try again.", null);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Received response from LLM service: {ResponseJson}", responseJson);

            using var doc = JsonDocument.Parse(responseJson);

            string? result = null;
            int[]? responseContext = null;

            if (isOllama)
            {
                // Ollama response format
                if (doc.RootElement.TryGetProperty("response", out var ollamaResponse))
                {
                    result = ollamaResponse.GetString();
                }

                // Extract context from Ollama response for token cache
                if (doc.RootElement.TryGetProperty("context", out var contextElement))
                {
                    try
                    {
                        var contextArray = contextElement.EnumerateArray().Select(x => x.GetInt32()).ToArray();
                        responseContext = contextArray.Length > 0 ? contextArray : null;
                        _logger.LogDebug("Received context from Ollama with {ContextLength} tokens", contextArray.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse context from Ollama response");
                    }
                }
            }
            else
            {
                // TGI response format (no context)
                if (doc.RootElement.TryGetProperty("generated_text", out var generatedText))
                {
                    result = generatedText.GetString();
                }
            }

            if (!string.IsNullOrEmpty(result))
            {
                _logger.LogDebug("Extracted generated text: {GeneratedText}", result);
                return (result, responseContext);
            }

            _logger.LogWarning("Unexpected response format from LLM service. Response: {ResponseJson}", responseJson);
            return ("Otrzymano niepoprawną odpowiedź z serwisu LLM.", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling LLM service with context");
            return ("Error occurred during LLM service communication.", null);
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3)); // very short timeout for health check

            var isOllama = _configuration.GetValue<bool>("Services:LlmService:IsOllama", false);
            var endpoint = isOllama ? "/api/tags" : "/health";

            var response = await _httpClient.GetAsync(endpoint, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException)
        {
            // Don't log cancellation as error, just return false
            return false;
        }
        catch (HttpRequestException)
        {
            // Connection issues are expected when service is down
            return false;
        }
        catch (Exception ex)
        {
            try
            {
                _logger.LogError(ex, "Health check failed for LLM service");
            }
            catch
            {
                // If logging fails, ignore it to prevent crash
            }
            return false;
        }
    }

    public async Task<string[]> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        var isOllama = _configuration.GetValue<bool>("Services:LlmService:IsOllama", false);
        try
        {
            if (isOllama)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(3)); // very fast timeout for dashboard
                var response = await _httpClient.GetAsync("/api/tags", cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to retrieve Ollama tags. Status: {StatusCode}", response.StatusCode);
                    return Array.Empty<string>();
                }
                var json = await response.Content.ReadAsStringAsync(cts.Token);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("models", out var modelsArray))
                {
                    var list = new List<string>();
                    foreach (var m in modelsArray.EnumerateArray())
                    {
                        if (m.TryGetProperty("name", out var nameProp))
                        {
                            var name = nameProp.GetString();
                            if (!string.IsNullOrWhiteSpace(name)) list.Add(name!);
                        }
                    }
                    return list.Distinct().OrderBy(n => n).ToArray();
                }
                return Array.Empty<string>();
            }
            else
            {
                // For non-Ollama (e.g. TGI) return configured model only
                var model = _configuration.GetValue<string>("Services:LlmService:Model", "unknown-model") ?? "unknown-model";
                return new[] { model };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available models from LLM service");
            return Array.Empty<string>();
        }
    }
}

public class ChatService : IChatService
{
    private static readonly List<ChatSession> _sessions = new();
    private static readonly Dictionary<string, List<ChatMessage>> _messages = new();

    private readonly Kernel _kernel;
    private readonly ISearchService _searchService;
    private readonly ILanguageService _languageService;
    private readonly ILogger<ChatService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ILlmService _llmService;

    public ChatService(
        Kernel kernel,
        ISearchService searchService,
        ILanguageService languageService,
        ILogger<ChatService> logger,
        IConfiguration configuration,
        ILlmService llmService)
    {
        _kernel = kernel;
        _searchService = searchService;
        _languageService = languageService;
        _logger = logger;
        _configuration = configuration;
        _llmService = llmService;
    }

    public Task<ChatSession[]> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_sessions.ToArray());
    }

    public Task<ChatSession> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid().ToString();
        var language = request.Language ?? _languageService.GetDefaultLanguage();
        var normalizedLanguage = _languageService.NormalizeLanguage(language);
        var sessionTitle = request.Title ?? _languageService.GetLocalizedString("session_labels", "new_conversation", normalizedLanguage);

        var session = new ChatSession(
            sessionId,
            sessionTitle,
            Array.Empty<ChatMessage>(),
            DateTime.Now,
            DateTime.Now
        );

        _sessions.Add(session);
        _messages[sessionId] = new List<ChatMessage>();

        return Task.FromResult(session);
    }

    public Task<ChatSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session == null) return Task.FromResult<ChatSession?>(null);

        var messages = _messages.GetValueOrDefault(sessionId, new List<ChatMessage>());
        var sessionWithMessages = session with { Messages = messages.ToArray() };
        return Task.FromResult<ChatSession?>(sessionWithMessages);
    }

    public async Task<ChatMessage> SendMessageAsync(string sessionId, ChatRequest request, CancellationToken cancellationToken = default)
    {
        if (!_messages.ContainsKey(sessionId))
            throw new ArgumentException("Session not found", nameof(sessionId));

        var maxMessageLength = _configuration.GetValue<int>("Chat:MaxMessageLength", 2000);
        if (request.Message.Length > maxMessageLength)
        {
            throw new ArgumentException($"Message too long. Maximum length is {maxMessageLength} characters.");
        }

        // Add user message
        var userMessage = new ChatMessage(
            Guid.NewGuid().ToString(),
            "user",
            request.Message,
            DateTime.Now
        );
        _messages[sessionId].Add(userMessage);

        try
        {
            // Search for relevant context only if document search is enabled
            RAG.Abstractions.Search.SearchResponse searchResults;
            if (request.UseDocumentSearch)
            {
                searchResults = await _searchService.SearchAsync(new RAG.Abstractions.Search.SearchRequest(
                    request.Message,
                    Filters: null,
                    Limit: 1, // Get only one document with the highest rating
                    Offset: 0
                ), cancellationToken);
            }
            else
            {
                // Empty search results when document search is disabled
                searchResults = new RAG.Abstractions.Search.SearchResponse(Array.Empty<RAG.Abstractions.Search.SearchResult>(), 0, 0, request.Message);
            }

            // Build context-aware prompt based on document search setting
            var prompt = BuildContextualPrompt(request.Message, searchResults.Results, _messages[sessionId], request.UseDocumentSearch);

            // Get previous Ollama context from the last assistant message for token cache continuation
            var lastAssistantMessage = _messages[sessionId]
                .Where(m => m.Role == "assistant" && m.OllamaContext != null)
                .LastOrDefault();
            var previousContext = lastAssistantMessage?.OllamaContext;

            // Check if Ollama is configured, if so use LLM service with context support
            var isOllama = _configuration.GetValue<bool>("Services:LlmService:IsOllama", false);
            string aiResponseContent;
            int[]? newOllamaContext = null;

            if (isOllama)
            {
                // Use LlmService with Ollama context support for token cache
                var (response, context) = await _llmService.GenerateResponseWithContextAsync(prompt, previousContext, cancellationToken);
                aiResponseContent = response;
                newOllamaContext = context;
                _logger.LogDebug("Generated response with Ollama context. Previous context length: {PreviousLength}, New context length: {NewLength}", 
                    previousContext?.Length ?? 0, newOllamaContext?.Length ?? 0);
            }
            else
            {
                // Fallback to Semantic Kernel for non-Ollama providers
                var aiResponse = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
                aiResponseContent = aiResponse.GetValue<string>() ??
                    _languageService.GetLocalizedErrorMessage("generation_failed", _languageService.GetDefaultLanguage());
            }

            var aiMessage = new ChatMessage(
                Guid.NewGuid().ToString(),
                "assistant",
                aiResponseContent,
                DateTime.Now,
                searchResults.Results.Length > 0 ? searchResults.Results : null,
                null,  // Metadata
                newOllamaContext  // Save Ollama context for future token cache usage
            );

            _messages[sessionId].Add(aiMessage);

            // Update session timestamp
            var sessionIndex = _sessions.FindIndex(s => s.Id == sessionId);
            if (sessionIndex >= 0)
            {
                _sessions[sessionIndex] = _sessions[sessionIndex] with { UpdatedAt = DateTime.Now };
            }

            return aiMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI response for session {SessionId}", sessionId);

            var errorMessage = new ChatMessage(
                Guid.NewGuid().ToString(),
                "assistant",
                _languageService.GetLocalizedErrorMessage("processing_error", _languageService.GetDefaultLanguage()),
                DateTime.Now
            );

            _messages[sessionId].Add(errorMessage);
            return errorMessage;
        }
    }

    public async Task<Models.MultilingualChatResponse> SendMultilingualMessageAsync(string sessionId, Models.MultilingualChatRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        if (!_messages.ContainsKey(sessionId))
            throw new ArgumentException("Session not found", nameof(sessionId));

        var maxMessageLength = _configuration.GetValue<int>("Chat:MaxMessageLength", 2000);
        if (request.Message.Length > maxMessageLength)
        {
            throw new ArgumentException($"Message too long. Maximum length is {maxMessageLength} characters.");
        }

        // Detect message language if not provided
        var detectedLanguage = string.IsNullOrEmpty(request.Language)
            ? _languageService.DetectLanguage(request.Message)
            : _languageService.NormalizeLanguage(request.Language);

        var responseLanguage = string.IsNullOrEmpty(request.ResponseLanguage)
            ? detectedLanguage
            : _languageService.NormalizeLanguage(request.ResponseLanguage);

        try
        {
            // Add user message
            var userMessage = new ChatMessage(
                Guid.NewGuid().ToString(),
                "user",
                request.Message,
                DateTime.Now
            );
            _messages[sessionId].Add(userMessage);

            // Search for relevant context with language consideration only if document search is enabled
            RAG.Abstractions.Search.SearchResponse searchResults;
            bool documentsAvailable = true;
            
            if (request.UseDocumentSearch)
            {
                var searchRequest = new RAG.Abstractions.Search.SearchRequest(
                    request.Message,
                    Filters: null, // TODO: Map metadata to SearchFilters if needed
                    Limit: 1, // Get only one document with the highest rating
                    Offset: 0
                );

                try
                {
                    searchResults = await _searchService.SearchAsync(searchRequest, cancellationToken);
                }
                catch (Features.Search.ElasticsearchUnavailableException ex)
                {
                    _logger.LogWarning(ex, "Document database unavailable, proceeding without context");
                    documentsAvailable = false;
                    searchResults = new RAG.Abstractions.Search.SearchResponse(Array.Empty<RAG.Abstractions.Search.SearchResult>(), 0, 0, request.Message);
                }
            }
            else
            {
                // Empty search results when document search is disabled
                searchResults = new RAG.Abstractions.Search.SearchResponse(Array.Empty<RAG.Abstractions.Search.SearchResult>(), 0, 0, request.Message);
                documentsAvailable = false; // Consider as unavailable when disabled
            }

            // Build multilingual context-aware prompt
            var prompt = BuildMultilingualContextualPrompt(
                request.Message,
                searchResults.Results,
                _messages[sessionId],
                detectedLanguage,
                responseLanguage,
                request.UseDocumentSearch,
                documentsAvailable
            );

            // Get previous Ollama context from the last assistant message for token cache continuation
            var lastAssistantMessage = _messages[sessionId]
                .Where(m => m.Role == "assistant" && m.OllamaContext != null)
                .LastOrDefault();
            var previousContext = lastAssistantMessage?.OllamaContext;

            // Check if Ollama is configured, if so use LLM service with context support
            var isOllama = _configuration.GetValue<bool>("Services:LlmService:IsOllama", false);
            string aiResponseContent;
            int[]? newOllamaContext = null;

            if (isOllama)
            {
                // Use LlmService with Ollama context support for token cache
                var (response, context) = await _llmService.GenerateResponseWithContextAsync(prompt, previousContext, cancellationToken);
                aiResponseContent = response;
                newOllamaContext = context;
                _logger.LogDebug("Generated multilingual response with Ollama context. Previous context length: {PreviousLength}, New context length: {NewLength}", 
                    previousContext?.Length ?? 0, newOllamaContext?.Length ?? 0);
            }
            else
            {
                // Fallback to Semantic Kernel for non-Ollama providers
                var aiResponse = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
                aiResponseContent = aiResponse.GetValue<string>() ??
                    _languageService.GetLocalizedErrorMessage("generation_failed", responseLanguage);
            }

            // Translate response if needed
            var wasTranslated = false;
            double? translationConfidence = null;

            if (request.EnableTranslation && detectedLanguage != responseLanguage)
            {
                try
                {
                    var translationResult = await _languageService.TranslateWithConfidenceAsync(
                        aiResponseContent, detectedLanguage, responseLanguage, cancellationToken);

                    aiResponseContent = translationResult.TranslatedText;
                    wasTranslated = true;
                    translationConfidence = translationResult.Confidence;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Translation failed, using original response");
                }
            }

            var aiMessage = new ChatMessage(
                Guid.NewGuid().ToString(),
                "assistant",
                aiResponseContent,
                DateTime.Now,
                searchResults.Results.Length > 0 ? searchResults.Results : null,
                null,  // Metadata
                newOllamaContext  // Save Ollama context for future token cache usage
            );

            _messages[sessionId].Add(aiMessage);

            // Update session timestamp
            var sessionIndex = _sessions.FindIndex(s => s.Id == sessionId);
            if (sessionIndex >= 0)
            {
                _sessions[sessionIndex] = _sessions[sessionIndex] with { UpdatedAt = DateTime.Now };
            }

            stopwatch.Stop();

            return new Models.MultilingualChatResponse
            {
                Response = aiResponseContent,
                SessionId = sessionId,
                DetectedLanguage = detectedLanguage,
                ResponseLanguage = responseLanguage,
                WasTranslated = wasTranslated,
                TranslationConfidence = translationConfidence,
                Sources = searchResults.Results.Length > 0 && documentsAvailable && request.UseDocumentSearch
                    ? searchResults.Results.Select(r => r.Source).ToList()
                    : null,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                Metadata = GetResponseMetadata(searchResults.Results.Length, request.EnableTranslation, documentsAvailable, responseLanguage, request.UseDocumentSearch)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating multilingual AI response for session {SessionId}", sessionId);

            stopwatch.Stop();

            return new Models.MultilingualChatResponse
            {
                Response = _languageService.GetLocalizedErrorMessage("processing_error", responseLanguage),
                SessionId = sessionId,
                DetectedLanguage = detectedLanguage,
                ResponseLanguage = responseLanguage,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    ["error"] = true,
                    ["errorMessage"] = ex.Message,
                    ["useDocumentSearch"] = request.UseDocumentSearch
                }
            };
        }
    }

    public Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var removed = _sessions.RemoveAll(s => s.Id == sessionId) > 0;
        _messages.Remove(sessionId);
        return Task.FromResult(removed);
    }

    private static string BuildContextualPrompt(string userMessage, RAG.Abstractions.Search.SearchResult[] searchResults, List<ChatMessage> conversationHistory, bool useDocumentSearch = true)
    {
        var promptBuilder = new StringBuilder();

        if (useDocumentSearch)
        {
            promptBuilder.AppendLine("Jesteś inteligentnym asystentem AI dla systemu RAG Suite. Odpowiadaj po polsku, profesjonalnie i pomocnie.");
            promptBuilder.AppendLine("Wykorzystuj kontekst z bazy wiedzy i historię rozmowy, aby udzielić dokładnej i przydatnej odpowiedzi.");
        }
        else
        {
            promptBuilder.AppendLine("Jesteś inteligentnym asystentem AI w firmie Ctronex. Odpowiadaj po polsku, profesjonalnie i pomocnie, bazując na swojej ogólnej wiedzy i historii rozmowy.");
            promptBuilder.AppendLine("Wykorzystuj historię rozmowy, aby udzielić dokładnej i pomocnej odpowiedzi bazując na swojej ogólnej wiedzy.");
        }
        promptBuilder.AppendLine();

        // Add context from search results only if document search is enabled
        if (useDocumentSearch && searchResults.Length > 0)
        {
            promptBuilder.AppendLine("=== KONTEKST Z BAZY WIEDZY ===");
            foreach (var result in searchResults.Take(3))
            {
                promptBuilder.AppendLine($"Dokument: {result.Title}");

                // Check if this is a reconstructed document
                bool isReconstructed = result.Metadata.ContainsKey("reconstructed") &&
                                     result.Metadata["reconstructed"] is bool reconstructed && reconstructed;

                if (isReconstructed)
                {
                    // For reconstructed documents, use more content (up to 2000 chars)
                    var contentLength = Math.Min(2000, result.Content.Length);
                    promptBuilder.AppendLine($"Treść (pełny dokument): {result.Content.Substring(0, contentLength)}");
                    if (result.Content.Length > contentLength)
                        promptBuilder.AppendLine("...");

                    var chunksCount = result.Metadata.ContainsKey("chunksFound") ? result.Metadata["chunksFound"] : "unknown";
                    promptBuilder.AppendLine($"[Zrekonstruowany z {chunksCount} fragmentów]");
                }
                else
                {
                    // For regular chunks, use the original 300 chars limit
                    promptBuilder.AppendLine($"Treść: {result.Content.Substring(0, Math.Min(300, result.Content.Length))}...");
                }

                promptBuilder.AppendLine($"Źródło: {result.Source}");
                promptBuilder.AppendLine();
            }
        }
        else if (!useDocumentSearch)
        {
            promptBuilder.AppendLine("=== UWAGA ===");
            promptBuilder.AppendLine("Wyszukiwanie dokumentów jest wyłączone w tej rozmowie. Odpowiedzi bazują tylko na ogólnej wiedzy.");
            promptBuilder.AppendLine();
        }

        // Add recent conversation history
        var recentMessages = conversationHistory.TakeLast(6).ToList();
        if (recentMessages.Count > 1)
        {
            promptBuilder.AppendLine("=== HISTORIA ROZMOWY ===");
            foreach (var msg in recentMessages.TakeLast(4))
            {
                var role = msg.Role == "user" ? "Użytkownik" : "Asystent";
                promptBuilder.AppendLine($"{role}: {msg.Content}");
            }
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine("=== AKTUALNE PYTANIE ===");
        promptBuilder.AppendLine(userMessage);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("=== INSTRUKCJE ===");
        promptBuilder.AppendLine("- Odpowiadaj w języku polskim");
        if (useDocumentSearch)
        {
            promptBuilder.AppendLine("- Bazuj na kontekście z bazy wiedzy, jeśli jest dostępny");
        }
        else
        {
            promptBuilder.AppendLine("- Bazuj na swojej ogólnej wiedzy i treningu");
        }
        promptBuilder.AppendLine("- Uwzględnij historię rozmowy dla spójności");
        if (useDocumentSearch)
        {
            promptBuilder.AppendLine("- Jeśli nie masz informacji w kontekście, powiedz o tym szczerze");
        }
        else
        {
            promptBuilder.AppendLine("- Jeśli nie masz informacji na dany temat, powiedz o tym szczerze");
        }
        promptBuilder.AppendLine("- Bądź konkretny i pomocny");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Odpowiedź:");

        return promptBuilder.ToString();
    }

    private string BuildMultilingualContextualPrompt(
        string userMessage,
        RAG.Abstractions.Search.SearchResult[] searchResults,
        List<ChatMessage> conversationHistory,
        string detectedLanguage,
        string responseLanguage,
        bool useDocumentSearch = true,
        bool documentsAvailable = true)
    {
        var promptBuilder = new StringBuilder();

        // Get localized system prompt and instructions based on document search setting
        var systemPrompt = useDocumentSearch 
            ? _languageService.GetLocalizedSystemPrompt("rag_assistant", responseLanguage)
            : _languageService.GetLocalizedString("system_prompts", "rag_assistant_no_docs", responseLanguage);
            
        var contextInstruction = useDocumentSearch
            ? _languageService.GetLocalizedString("system_prompts", "context_instruction", responseLanguage)
            : _languageService.GetLocalizedString("system_prompts", "context_instruction_no_docs", responseLanguage);

        promptBuilder.AppendLine(systemPrompt);
        promptBuilder.AppendLine(contextInstruction);
        promptBuilder.AppendLine();

        // Add context from search results with translation note if needed
        if (useDocumentSearch && documentsAvailable && searchResults.Length > 0)
        {
            var contextLabel = _languageService.GetLocalizedString("system_prompts", "knowledge_base_context", responseLanguage);
            promptBuilder.AppendLine($"=== {contextLabel} ===");

            foreach (var result in searchResults.Take(3))
            {
                var titleLabel = _languageService.GetLocalizedString("system_prompts", "document", responseLanguage);
                var contentLabel = _languageService.GetLocalizedString("system_prompts", "content", responseLanguage);
                var sourceLabel = _languageService.GetLocalizedString("system_prompts", "source", responseLanguage);

                promptBuilder.AppendLine($"{titleLabel}: {result.Title}");

                // Check if this is a reconstructed document
                bool isReconstructed = result.Metadata.ContainsKey("reconstructed") &&
                                     result.Metadata["reconstructed"] is bool reconstructed && reconstructed;

                if (isReconstructed)
                {
                    // For reconstructed documents, use more content (up to 2000 chars)
                    var contentLength = Math.Min(2000, result.Content.Length);
                    promptBuilder.AppendLine($"{contentLabel} (pełny dokument): {result.Content.Substring(0, contentLength)}");
                    if (result.Content.Length > contentLength)
                        promptBuilder.AppendLine("...");

                    var chunksCount = result.Metadata.ContainsKey("chunksFound") ? result.Metadata["chunksFound"] : "unknown";
                    var reconstructedLabel = _languageService.GetLocalizedString("system_prompts", "reconstructed_from_chunks", responseLanguage)
                        ?? "[Zrekonstruowany z {0} fragmentów]";
                    promptBuilder.AppendLine(string.Format(reconstructedLabel, chunksCount));
                }
                else
                {
                    // For regular chunks, use the original 300 chars limit
                    promptBuilder.AppendLine($"{contentLabel}: {result.Content.Substring(0, Math.Min(300, result.Content.Length))}...");
                }

                promptBuilder.AppendLine($"{sourceLabel}: {result.Source}");
                promptBuilder.AppendLine();
            }
        }
        else if (!useDocumentSearch)
        {
            // Add note about document search being disabled
            var noSearchNote = _languageService.GetLocalizedString("system_prompts", "no_document_search_note", responseLanguage)
                ?? "Note: Document search is disabled for this conversation.";
            promptBuilder.AppendLine($"=== UWAGA ===");
            promptBuilder.AppendLine(noSearchNote);
            promptBuilder.AppendLine();
        }
        else if (useDocumentSearch && !documentsAvailable)
        {
            // Add note about document database unavailability
            var unavailableNote = _languageService.GetLocalizedString("system_prompts", "documents_unavailable", responseLanguage)
                ?? "Note: The document database is currently unavailable. Responses will be generated without reference documents.";
            promptBuilder.AppendLine($"=== UWAGA ===");
            promptBuilder.AppendLine(unavailableNote);
            promptBuilder.AppendLine();
        }

        // Add recent conversation history
        var recentMessages = conversationHistory.TakeLast(6).ToList();
        if (recentMessages.Count > 1)
        {
            var historyLabel = _languageService.GetLocalizedString("system_prompts", "conversation_history", responseLanguage);
            promptBuilder.AppendLine($"=== {historyLabel} ===");

            var userLabel = _languageService.GetLocalizedString("ui_labels", "user", responseLanguage);
            var assistantLabel = _languageService.GetLocalizedString("ui_labels", "assistant", responseLanguage);

            foreach (var msg in recentMessages.TakeLast(4))
            {
                var role = msg.Role == "user" ? userLabel : assistantLabel;
                promptBuilder.AppendLine($"{role}: {msg.Content}");
            }
            promptBuilder.AppendLine();
        }

        // Add current question
        var questionLabel = _languageService.GetLocalizedString("system_prompts", "current_question", responseLanguage);
        promptBuilder.AppendLine($"=== {questionLabel} ===");
        promptBuilder.AppendLine(userMessage);
        promptBuilder.AppendLine();

        // Add localized instructions based on document search setting
        var instructionsLabel = _languageService.GetLocalizedString("ui_labels", "instructions", responseLanguage);
        promptBuilder.AppendLine($"=== {instructionsLabel} ===");
        
        var respondInLanguage = _languageService.GetLocalizedString("instructions", "respond_in_language", responseLanguage);
        promptBuilder.AppendLine($"- {respondInLanguage}");
        
        if (useDocumentSearch)
        {
            var useKnowledgeBase = _languageService.GetLocalizedString("instructions", "use_knowledge_base", responseLanguage);
            promptBuilder.AppendLine($"- {useKnowledgeBase}");
        }
        else
        {
            var useGeneralKnowledge = _languageService.GetLocalizedString("instructions", "use_general_knowledge", responseLanguage);
            promptBuilder.AppendLine($"- {useGeneralKnowledge}");
        }
        
        var considerHistory = _languageService.GetLocalizedString("instructions", "consider_history", responseLanguage);
        promptBuilder.AppendLine($"- {considerHistory}");
        
        var beHonest = useDocumentSearch 
            ? _languageService.GetLocalizedString("instructions", "be_honest", responseLanguage)
            : _languageService.GetLocalizedString("instructions", "be_honest_no_docs", responseLanguage);
        promptBuilder.AppendLine($"- {beHonest}");
        
        var beHelpful = _languageService.GetLocalizedString("instructions", "be_helpful", responseLanguage);
        promptBuilder.AppendLine($"- {beHelpful}");
        promptBuilder.AppendLine();

        // Add language-specific note if translation is involved
        if (detectedLanguage != responseLanguage)
        {
            var translationNote = _languageService.GetLocalizedString("system_prompts", "translation_note", responseLanguage);
            promptBuilder.AppendLine($"[{translationNote}: {detectedLanguage} → {responseLanguage}]");
            promptBuilder.AppendLine();
        }

        var responseLabel = _languageService.GetLocalizedString("system_prompts", "response", responseLanguage);
        promptBuilder.AppendLine($"{responseLabel}:");

        return promptBuilder.ToString();
    }

    private Dictionary<string, object> GetResponseMetadata(int searchResultsCount, bool enabledTranslation, bool documentsAvailable, string responseLanguage, bool useDocumentSearch = true)
    {
        var metadata = new Dictionary<string, object>
        {
            ["searchResultsCount"] = searchResultsCount,
            ["enabledTranslation"] = enabledTranslation,
            ["documentsAvailable"] = documentsAvailable,
            ["useDocumentSearch"] = useDocumentSearch
        };

        if (!useDocumentSearch)
        {
            var disabledMessage = _languageService.GetLocalizedString("error_messages", "document_search_disabled", responseLanguage)
                ?? "Document search is disabled for this conversation";
            metadata["documentSearchDisabledMessage"] = disabledMessage;
        }
        else if (!documentsAvailable)
        {
            var unavailableMessage = _languageService.GetLocalizedString("error_messages", "documents_unavailable", responseLanguage)
                ?? "Document database is currently unavailable";
            metadata["databaseUnavailableMessage"] = unavailableMessage;
        }

        return metadata;
    }
}
