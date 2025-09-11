using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RAG.Orchestrator.Api.Models;
using RAG.Orchestrator.Api.Models.Configuration;
using RAG.Orchestrator.Api.Services;

public class LlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly LlmEndpointConfig _config;
    private readonly ILogger<LlmService> _logger;

    public LlmService(HttpClient httpClient, IOptions<LlmEndpointConfig> config, ILogger<LlmService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_config.Url);
        _httpClient.Timeout = TimeSpan.FromMinutes(_config.TimeoutMinutes);

        _logger.LogDebug("LlmService configured with URL: {Url}, Model: {Model}, Timeout: {Timeout}min",
            _config.Url, _config.Model, _config.TimeoutMinutes);
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
            var maxTokens = _config.MaxTokens;
            var temperature = _config.Temperature;
            var model = _config.Model;
            var isOllama = _config.IsOllama;

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
                var topP = 0.9; // Default value since not in LlmEndpointConfig
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

            var isOllama = _config.IsOllama;
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
        var isOllama = _config.IsOllama;
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
                var model = _config.Model;
                return new[] { model };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available models from LLM service");
            return Array.Empty<string>();
        }
    }

    public async Task<string> ChatWithHistoryAsync(IEnumerable<LlmChatMessage> messageHistory, string userMessage, string language = "en", CancellationToken cancellationToken = default)
    {
        try
        {
            var maxTokens = _config.MaxTokens;
            var temperature = _config.Temperature;
            var model = _config.Model;
            var isOllama = _config.IsOllama;

            if (!isOllama)
            {
                _logger.LogWarning("Chat API is only supported for Ollama. Falling back to generate endpoint.");
                return await GenerateResponseAsync(userMessage, cancellationToken);
            }

            // Build messages array for /api/chat
            var messages = new List<LlmChatMessage>();

            // Add system message as first message if needed

            var systemMessage = await GetSystemMessageAsync(language, cancellationToken);
            if (!string.IsNullOrEmpty(systemMessage))
            {
                messages.Add(new LlmChatMessage { Role = "system", Content = systemMessage });
                _logger.LogDebug("Added system message as first message in chat history");
            }

            // Add conversation history
            messages.AddRange(messageHistory);

            // Add current user message
            messages.Add(new LlmChatMessage { Role = "user", Content = userMessage });

            var request = new
            {
                model = model,
                messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
                stream = false,
                options = new
                {
                    temperature = temperature,
                    num_predict = maxTokens
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending chat request to Ollama with {MessageCount} messages", messages.Count);

            var response = await _httpClient.PostAsync(_config.ChatEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Ollama chat API returned error: {StatusCode}, Content: {ErrorContent}", response.StatusCode, errorContent);
                return "Sorry, there was a problem generating the response. Please try again.";
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Received chat response from Ollama: {ResponseJson}", responseJson);

            using var doc = JsonDocument.Parse(responseJson);

            // Parse /api/chat response format
            if (doc.RootElement.TryGetProperty("message", out var messageElement))
            {
                if (messageElement.TryGetProperty("content", out var contentElement))
                {
                    var result = contentElement.GetString();
                    if (!string.IsNullOrEmpty(result))
                    {
                        _logger.LogDebug("Extracted chat response: {GeneratedText}", result);
                        return result;
                    }
                }
            }

            _logger.LogWarning("Unexpected response format from Ollama chat API. Response: {ResponseJson}", responseJson);
            return "Otrzymano niepoprawną odpowiedź z serwisu Ollama.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Ollama chat API");
            return "Error occurred during Ollama chat communication.";
        }
    }

    public async Task<string> GetSystemMessageAsync(string language = "en", CancellationToken cancellationToken = default)
    {
        try
        {
            // Map language codes to supported languages
            var supportedLanguages = new[] { "pl", "en", "hu", "nl", "ro" };
            var targetLanguage = supportedLanguages.Contains(language.ToLower()) ? language.ToLower() : "en";

            var fileName = $"system_{targetLanguage}.md";
            var filePath = Path.Combine(AppContext.BaseDirectory, "Localization", fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("System message file not found: {FilePath}. Using fallback.", filePath);
                return GetFallbackSystemMessage();
            }

            var jsonContent = await File.ReadAllTextAsync(filePath, cancellationToken);
            var systemMessageModel = JsonSerializer.Deserialize<SystemMessageModel>(jsonContent);

            if (systemMessageModel?.system_message != null)
            {
                _logger.LogDebug("Loaded system message for language: {Language}", targetLanguage);
                return systemMessageModel.system_message;
            }

            _logger.LogWarning("Invalid system message format in file: {FilePath}", filePath);
            return GetFallbackSystemMessage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading system message for language: {Language}", language);
            return GetFallbackSystemMessage();
        }
    }

    private string GetFallbackSystemMessage()
    {
        return "I am Ctronex's AI assistant, specializing in the RAG Suite system - an advanced tool for organizational knowledge management. I can assist in finding information in the organizational knowledge base, answering questions about procedures, policies, and technical documentation.";
    }
}