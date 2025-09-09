using Microsoft.Extensions.Options;
using RAG.Orchestrator.Api.Features.Embeddings.Models;
using System.Text;
using System.Text.Json;

namespace RAG.Orchestrator.Api.Features.Embeddings;

public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmbeddingService> _logger;

    public int EmbeddingDimension => 768; // all-MiniLM-L6-v2 embedding dimension

    public EmbeddingService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<EmbeddingService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var embeddingServiceUrl = _configuration["Services:EmbeddingService:Url"] ?? "http://localhost:8580";
        _httpClient.BaseAddress = new Uri(embeddingServiceUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(2);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        try
        {
            _logger.LogDebug("Generating embedding for text of length: {Length}", text.Length);

            var request = new EmbeddingRequest { Inputs = text };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/embed", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();

            // The response is a direct array of arrays: [[embedding_values]]
            var embeddingArrays = JsonSerializer.Deserialize<float[][]>(responseJson);

            if (embeddingArrays == null || embeddingArrays.Length == 0)
            {
                throw new InvalidOperationException("Empty embedding response");
            }

            var embedding = embeddingArrays[0];
            _logger.LogDebug("Generated embedding with dimension: {Dimension}", embedding.Length);

            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for text: {Text}", text.Substring(0, Math.Min(100, text.Length)));
            throw;
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Embedding service health check failed");
            return false;
        }
    }
}
