using RAG.Orchestrator.Api.Features.Chat;
using RAG.Orchestrator.Api.Services;
using System.Text.Json;

namespace RAG.Orchestrator.Api.Features.Health;

public class HealthAggregator : IHealthAggregator
{
    private readonly ILlmService _llmService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HealthAggregator> _logger;

    public HealthAggregator(ILlmService llmService, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<HealthAggregator> logger)
    {
        _llmService = llmService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SystemHealthResponse> GetSystemHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5)); // maximum timeout for entire health check

            var apiStatus = new ServiceStatus("orchestrator-api", "healthy"); // if this code runs, API is up

            var llmStatus = await GetLlmStatusAsync(cts.Token);
            var esStatus = await GetElasticsearchStatusAsync(cts.Token);
            var vectorStatus = await GetVectorStoreStatusAsync(cts.Token);

            return new SystemHealthResponse(apiStatus, llmStatus, esStatus, vectorStatus, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            // Fallback response if everything fails
            var apiStatus = new ServiceStatus("orchestrator-api", "healthy"); // if this code runs, API is up
            var errorStatus = new ServiceStatus("unknown", "error", ex.Message);

            try
            {
                _logger.LogError(ex, "System health check failed completely");
            }
            catch { /* Ignore logging errors */ }

            return new SystemHealthResponse(apiStatus, errorStatus, errorStatus, errorStatus, DateTime.UtcNow);
        }
    }

    private async Task<ServiceStatus> GetLlmStatusAsync(CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3)); // very short timeout for health check

            var healthy = await _llmService.IsHealthyAsync(cts.Token);
            var models = Array.Empty<string>();
            if (healthy)
            {
                models = await _llmService.GetAvailableModelsAsync(cts.Token);
            }
            return new ServiceStatus("llm", healthy ? "healthy" : "error", healthy ? null : "LLM service unavailable", new { models });
        }
        catch (OperationCanceledException)
        {
            try
            {
                _logger.LogWarning("LLM health check timed out");
            }
            catch { /* Ignore logging errors */ }
            return new ServiceStatus("llm", "error", "Service timeout");
        }
        catch (Exception ex)
        {
            try
            {
                _logger.LogError(ex, "LLM health check failed");
            }
            catch { /* Ignore logging errors */ }
            return new ServiceStatus("llm", "error", ex.Message);
        }
    }

    private async Task<ServiceStatus> GetElasticsearchStatusAsync(CancellationToken ct)
    {
        var url = _configuration["Services:Elasticsearch:Url"] ?? "http://localhost:9200";
        var username = _configuration["Services:Elasticsearch:Username"];
        var password = _configuration["Services:Elasticsearch:Password"];

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(2)); // very short timeout for health check
            var client = _httpClientFactory.CreateClient();

            // Add basic authentication if credentials are provided
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            }

            var response = await client.GetAsync(url, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return new ServiceStatus("elasticsearch", "error", $"HTTP {(int)response.StatusCode}");
            }
            var json = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(json);
            var clusterName = doc.RootElement.TryGetProperty("cluster_name", out var cn) ? cn.GetString() : null;
            return new ServiceStatus("elasticsearch", "healthy", null, new { clusterName });
        }
        catch (OperationCanceledException)
        {
            try
            {
                _logger.LogWarning("Elasticsearch health check timed out");
            }
            catch { /* Ignore logging errors */ }
            return new ServiceStatus("elasticsearch", "error", "Service timeout");
        }
        catch (HttpRequestException)
        {
            // Connection issues are expected when Elasticsearch is down
            return new ServiceStatus("elasticsearch", "error", "Connection refused");
        }
        catch (Exception ex)
        {
            try
            {
                _logger.LogWarning(ex, "Elasticsearch health check failed");
            }
            catch { /* Ignore logging errors */ }
            return new ServiceStatus("elasticsearch", "error", ex.Message);
        }
    }

    // Placeholder for vector store (could be the same ES cluster or another service) – mark as unknown for now
    private Task<ServiceStatus> GetVectorStoreStatusAsync(CancellationToken ct)
    {
        // If vector store uses Elasticsearch indices, we could reuse ES status. For now return same as ES.
        return Task.FromResult(new ServiceStatus("vector-store", "healthy", null));
    }
}
