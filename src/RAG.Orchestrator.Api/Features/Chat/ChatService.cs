using RAG.Orchestrator.Api.Features.Chat;
using RAG.Orchestrator.Api.Features.Search;
using System.Text;
using System.Text.Json;

namespace RAG.Orchestrator.Api.Features.Chat;

public interface IChatService
{
    Task<ChatSession[]> GetSessionsAsync(CancellationToken cancellationToken = default);
    Task<ChatSession> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    Task<ChatSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<ChatMessage> SendMessageAsync(string sessionId, ChatRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}

public interface ILlmService
{
    Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
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
        
        var baseUrl = configuration["Services:LlmService:Url"] ?? "http://localhost:8581";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            var maxTokens = _configuration.GetValue<int>("Services:LlmService:MaxTokens", 1024);
            var temperature = _configuration.GetValue<double>("Services:LlmService:Temperature", 0.7);
            var topP = _configuration.GetValue<double>("Services:LlmService:TopP", 0.9);

            var request = new
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

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending request to LLM service: {Json}", json);
            
            var response = await _httpClient.PostAsync("/generate", content, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("LLM service returned error: {StatusCode}, Content: {ErrorContent}", response.StatusCode, errorContent);
                return "Przepraszam, wystąpił problem z generowaniem odpowiedzi. Spróbuj ponownie.";
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Received response from LLM service: {ResponseJson}", responseJson);
            
            using var doc = JsonDocument.Parse(responseJson);
            
            if (doc.RootElement.TryGetProperty("generated_text", out var generatedText))
            {
                var result = generatedText.GetString() ?? "Nie udało się wygenerować odpowiedzi.";
                _logger.LogDebug("Extracted generated text: {GeneratedText}", result);
                return result;
            }

            _logger.LogWarning("Unexpected response format from LLM service. Response: {ResponseJson}", responseJson);
            return "Otrzymano niepoprawną odpowiedź z serwisu LLM.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling LLM service");
            return "Wystąpił błąd podczas komunikacji z serwisem LLM.";
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for LLM service");
            return false;
        }
    }
}

public class ChatService : IChatService
{
    private static readonly List<ChatSession> _sessions = new();
    private static readonly Dictionary<string, List<ChatMessage>> _messages = new();
    
    private readonly ILlmService _llmService;
    private readonly ISearchService _searchService;
    private readonly ILogger<ChatService> _logger;
    private readonly IConfiguration _configuration;

    public ChatService(
        ILlmService llmService, 
        ISearchService searchService, 
        ILogger<ChatService> logger,
        IConfiguration configuration)
    {
        _llmService = llmService;
        _searchService = searchService;
        _logger = logger;
        _configuration = configuration;
    }

    public Task<ChatSession[]> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_sessions.ToArray());
    }

    public Task<ChatSession> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid().ToString();
        var session = new ChatSession(
            sessionId,
            request.Title ?? "Nowa rozmowa",
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
            // Search for relevant context
            var searchResults = await _searchService.SearchAsync(new SearchRequest(
                request.Message,
                Filters: null,
                Limit: 3,
                Offset: 0
            ), cancellationToken);

            // Build context-aware prompt
            var prompt = BuildContextualPrompt(request.Message, searchResults.Results, _messages[sessionId]);

            // Generate AI response
            var aiResponseContent = await _llmService.GenerateResponseAsync(prompt, cancellationToken);

            var aiMessage = new ChatMessage(
                Guid.NewGuid().ToString(),
                "assistant",
                aiResponseContent,
                DateTime.Now,
                searchResults.Results.Length > 0 ? searchResults.Results : null
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
                "Przepraszam, wystąpił problem podczas generowania odpowiedzi. Spróbuj ponownie za chwilę.",
                DateTime.Now
            );
            
            _messages[sessionId].Add(errorMessage);
            return errorMessage;
        }
    }

    public Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var removed = _sessions.RemoveAll(s => s.Id == sessionId) > 0;
        _messages.Remove(sessionId);
        return Task.FromResult(removed);
    }

    private static string BuildContextualPrompt(string userMessage, SearchResult[] searchResults, List<ChatMessage> conversationHistory)
    {
        var promptBuilder = new StringBuilder();
        
        promptBuilder.AppendLine("Jesteś inteligentnym asystentem AI dla systemu RAG Suite. Odpowiadaj po polsku, profesjonalnie i pomocnie.");
        promptBuilder.AppendLine();
        
        // Add context from search results
        if (searchResults.Length > 0)
        {
            promptBuilder.AppendLine("Kontekst z bazy wiedzy:");
            foreach (var result in searchResults.Take(3))
            {
                promptBuilder.AppendLine($"- {result.Title}: {result.Content.Substring(0, Math.Min(200, result.Content.Length))}...");
            }
            promptBuilder.AppendLine();
        }
        
        // Add recent conversation history
        var recentMessages = conversationHistory.TakeLast(6).ToList();
        if (recentMessages.Count > 1)
        {
            promptBuilder.AppendLine("Historia rozmowy:");
            foreach (var msg in recentMessages.TakeLast(4))
            {
                var role = msg.Role == "user" ? "Użytkownik" : "Asystent";
                promptBuilder.AppendLine($"{role}: {msg.Content}");
            }
            promptBuilder.AppendLine();
        }
        
        promptBuilder.AppendLine($"Aktualne pytanie użytkownika: {userMessage}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Odpowiedź:");
        
        return promptBuilder.ToString();
    }
}
