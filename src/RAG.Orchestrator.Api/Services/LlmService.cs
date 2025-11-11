using RAG.Orchestrator.Api.Models;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace RAG.Orchestrator.Api.Services;

public class LlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly IGlobalSettingsCache _globalSettingsCache;
    private readonly ILogger<LlmService> _logger;
    private readonly ConcurrentDictionary<string, string> _systemMessageCache = new();
    private readonly string[] _supportedLanguages = { "pl", "en", "hu", "nl", "ro" };

    private const string DefaultSystemMessage = "I am Ctronex's AI assistant, specializing in the RAG Suite system - an advanced tool for organizational knowledge management. I can assist in finding information in the organizational knowledge base, answering questions about procedures, policies, and technical documentation.";

    public LlmService(HttpClient httpClient, IGlobalSettingsCache globalSettingsCache, ILogger<LlmService> logger)
    {
        _httpClient = httpClient;
        _globalSettingsCache = globalSettingsCache;
        _logger = logger;
    }

    private async Task<LlmSettings> GetSettingsAsync()
    {
        var settings = await _globalSettingsCache.GetLlmSettingsAsync();
        if (settings == null)
        {
            throw new InvalidOperationException("LLM settings not found. Please initialize settings first.");
        }

        // Update HttpClient settings
        _httpClient.BaseAddress = new Uri(settings.Url);
        _httpClient.Timeout = TimeSpan.FromMinutes(settings.TimeoutMinutes);

        return settings;
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
            var settings = await GetSettingsAsync();
            var (request, endpoint) = BuildGenerateRequest(settings, prompt, context);

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

            return ParseGenerateResponse(responseJson, settings.IsOllama);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "LLM service request timed out");
            return ("Request timed out. Please try again.", null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while calling LLM service");
            return ("Network error occurred. Please check LLM service availability.", null);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM service response");
            return ("Failed to parse response from LLM service.", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling LLM service with context");
            return ("Error occurred during LLM service communication.", null);
        }
    }

    private static (object request, string endpoint) BuildGenerateRequest(LlmSettings settings, string prompt, int[]? context)
    {
        if (settings.IsOllama)
        {
            var baseRequest = new
            {
                model = settings.Model,
                prompt,
                stream = false,
                options = new
                {
                    temperature = settings.Temperature,
                    num_predict = settings.MaxTokens
                }
            };

            // Add context if provided (for Ollama token cache)
            object request = context is { Length: > 0 }
                ? new
                {
                    baseRequest.model,
                    baseRequest.prompt,
                    baseRequest.stream,
                    context,
                    baseRequest.options
                }
                : baseRequest;

            return (request, "/api/generate");
        }
        else
        {
            // TGI API format (no context support)
            var request = new
            {
                inputs = prompt,
                parameters = new
                {
                    max_new_tokens = settings.MaxTokens,
                    temperature = settings.Temperature,
                    top_p = 0.9,
                    do_sample = true,
                    return_full_text = false
                }
            };
            return (request, "/generate");
        }
    }

    private (string response, int[]? context) ParseGenerateResponse(string responseJson, bool isOllama)
    {
        using var doc = JsonDocument.Parse(responseJson);

        if (isOllama)
        {
            string? result = null;
            int[]? responseContext = null;

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

            if (!string.IsNullOrEmpty(result))
            {
                _logger.LogDebug("Extracted generated text: {GeneratedText}", result);
                return (result, responseContext);
            }
        }
        else
        {
            // TGI response format (no context)
            if (doc.RootElement.TryGetProperty("generated_text", out var generatedText))
            {
                var result = generatedText.GetString();
                if (!string.IsNullOrEmpty(result))
                {
                    _logger.LogDebug("Extracted generated text: {GeneratedText}", result);
                    return (result, null);
                }
            }
        }

        _logger.LogWarning("Unexpected response format from LLM service. Response: {ResponseJson}", responseJson);
        return ("Received an incorrect response from the LLM service.", null);
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            var settings = await GetSettingsAsync();
            var endpoint = settings.IsOllama ? "/api/tags" : "/health";

            var response = await _httpClient.GetAsync(endpoint, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for LLM service");
            return false;
        }
    }

    public async Task<string[]> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync();

        if (!settings.IsOllama)
        {
            return new[] { settings.Model };
        }

        return await FetchOllamaModelsAsync(_httpClient, null, cancellationToken);
    }

    public async Task<string[]> GetAvailableModelsAsync(string url, bool isOllama, CancellationToken cancellationToken = default)
    {
        if (!isOllama)
        {
            return Array.Empty<string>();
        }

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(url),
            Timeout = TimeSpan.FromSeconds(5)
        };

        return await FetchOllamaModelsAsync(httpClient, url, cancellationToken);
    }

    private async Task<string[]> FetchOllamaModelsAsync(HttpClient httpClient, string? url, CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            var response = await httpClient.GetAsync("/api/tags", cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                var logMessage = url != null
                    ? $"Failed to retrieve Ollama tags from {url}. Status: {response.StatusCode}"
                    : $"Failed to retrieve Ollama tags. Status: {response.StatusCode}";
                _logger.LogWarning(logMessage);
                return Array.Empty<string>();
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            return ParseOllamaModels(json);
        }
        catch (Exception ex)
        {
            var logMessage = url != null
                ? $"Error retrieving available models from LLM service at {url}"
                : "Error retrieving available models from LLM service";
            _logger.LogError(ex, logMessage);
            return Array.Empty<string>();
        }
    }

    private static string[] ParseOllamaModels(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("models", out var modelsArray))
        {
            return Array.Empty<string>();
        }

        var modelNames = modelsArray.EnumerateArray()
            .Where(m => m.TryGetProperty("name", out var nameProp) && !string.IsNullOrWhiteSpace(nameProp.GetString()))
            .Select(m => m.GetProperty("name").GetString()!)
            .Distinct()
            .OrderBy(n => n)
            .ToArray();

        return modelNames;
    }

    public Task<string> ChatWithHistoryAsync(
        IEnumerable<LlmChatMessage> messageHistory,
        string userMessage,
        string language = "en",
        CancellationToken cancellationToken = default)
    {
        return ChatWithHistoryAsync(messageHistory, userMessage, language, null, null, null, null, cancellationToken);
    }

    public async Task<string> ChatWithHistoryAsync(
        IEnumerable<LlmChatMessage> messageHistory,
        string userMessage,
        string language,
        string? firstName,
        string? lastName,
        string? email,
        string? role,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await GetSettingsAsync();

            if (!settings.IsOllama)
            {
                _logger.LogWarning("Chat API is only supported for Ollama. Falling back to generate endpoint.");
                return await GenerateResponseAsync(userMessage, cancellationToken);
            }

            var messages = await BuildChatMessagesAsync(messageHistory, userMessage, language, firstName, lastName, email, role, cancellationToken);

            var request = new
            {
                model = settings.Model,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
                stream = false,
                options = new
                {
                    temperature = settings.Temperature,
                    num_predict = settings.MaxTokens
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending chat request to Ollama with {MessageCount} messages", messages.Count);

            var response = await _httpClient.PostAsync(settings.ChatEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Ollama chat API returned error: {StatusCode}, Content: {ErrorContent}", response.StatusCode, errorContent);
                return "Sorry, there was a problem generating the response. Please try again.";
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Received chat response from Ollama: {ResponseJson}", responseJson);

            return ParseChatResponse(responseJson);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Ollama chat request timed out");
            return "Request timed out. Please try again.";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while calling Ollama chat API");
            return "Network error occurred. Please check Ollama service availability.";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Ollama chat response");
            return "Failed to parse response from Ollama service.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Ollama chat API");
            return "Error occurred during Ollama chat communication.";
        }
    }

    private async Task<List<LlmChatMessage>> BuildChatMessagesAsync(
        IEnumerable<LlmChatMessage> messageHistory,
        string userMessage,
        string language,
        string? firstName,
        string? lastName,
        string? email,
        string? role,
        CancellationToken cancellationToken)
    {
        var messages = new List<LlmChatMessage>();

        var systemMessage = await GetSystemMessageAsync(language, firstName, lastName, email, role, cancellationToken);
        if (!string.IsNullOrEmpty(systemMessage))
        {
            messages.Add(new LlmChatMessage { Role = "system", Content = systemMessage });
            _logger.LogDebug("Added system message as first message in chat history");
        }

        messages.AddRange(messageHistory);
        messages.Add(new LlmChatMessage { Role = "user", Content = userMessage });

        return messages;
    }

    private string ParseChatResponse(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);

        if (doc.RootElement.TryGetProperty("message", out var messageElement) &&
            messageElement.TryGetProperty("content", out var contentElement))
        {
            var result = contentElement.GetString();
            if (!string.IsNullOrEmpty(result))
            {
                _logger.LogDebug("Extracted chat response: {GeneratedText}", result);
                return result;
            }
        }

        _logger.LogWarning("Unexpected response format from Ollama chat API. Response: {ResponseJson}", responseJson);
        return "Received an incorrect response from the Ollama service.";
    }

    public Task<string> GetSystemMessageAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        return GetSystemMessageAsync(language, null, null, null, null, cancellationToken);
    }

    public async Task<string> GetSystemMessageAsync(
        string language,
        string? firstName,
        string? lastName,
        string? email,
        string? role,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var targetLanguage = _supportedLanguages.Contains(language.ToLower()) ? language.ToLower() : "en";
            var cacheKey = $"{targetLanguage}_{firstName}_{lastName}_{email}_{role}";

            // Check cache first
            if (_systemMessageCache.TryGetValue(cacheKey, out var cachedMessage))
            {
                return cachedMessage;
            }

            var message = await LoadSystemMessageAsync(targetLanguage, firstName, lastName, email, role, cancellationToken);

            // Cache the loaded message
            _systemMessageCache.TryAdd(cacheKey, message);

            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading system message for language: {Language}", language);
            return DefaultSystemMessage;
        }
    }

    private async Task<string> LoadSystemMessageAsync(
        string targetLanguage,
        string? firstName,
        string? lastName,
        string? email,
        string? role,
        CancellationToken cancellationToken)
    {
        var fileName = $"system_{targetLanguage}.md";
        var filePath = Path.Combine(AppContext.BaseDirectory, "Localization", fileName);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("System message file not found: {FilePath}. Using fallback.", filePath);
            return DefaultSystemMessage;
        }

        var systemMessage = await File.ReadAllTextAsync(filePath, cancellationToken);

        if (string.IsNullOrEmpty(systemMessage))
        {
            _logger.LogWarning("Empty system message in file: {FilePath}", filePath);
            return DefaultSystemMessage;
        }

        // Replace user information placeholders if any are provided
        if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName) ||
            !string.IsNullOrEmpty(email) || !string.IsNullOrEmpty(role))
        {
            systemMessage = systemMessage
                .Replace("{FirstName}", firstName ?? string.Empty)
                .Replace("{LastName}", lastName ?? string.Empty)
                .Replace("{Email}", email ?? string.Empty)
                .Replace("{Role}", role ?? string.Empty);
        }

        _logger.LogDebug("Loaded system message for language: {Language}", targetLanguage);
        return systemMessage;
    }
}