using System.Text;
using System.Text.Json;
using RAG.Collector.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RAG.Collector.Config;

namespace RAG.Collector.Embeddings;

/// <summary>
/// HTTP-based embedding provider for external embedding services
/// </summary>
public class HttpEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpEmbeddingProvider> _logger;
    private readonly CollectorOptions _options;

    public HttpEmbeddingProvider(
        HttpClient httpClient,
        ILogger<HttpEmbeddingProvider> logger,
        IOptions<CollectorOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public string ModelName => _options.EmbeddingModelName;
    public int MaxTokens => 512; // Safe default for most models
    public int VectorDimensions => 768; // Common dimension for multilingual-e5-small

    public async Task<EmbeddingResult> GenerateEmbeddingAsync(TextChunk chunk, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var request = new
            {
                inputs = chunk.Content,
                truncate = true
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Generating embedding for chunk {ChunkId} (length: {Length} chars, ~{Tokens} tokens)",
                chunk.Id, chunk.Content.Length, chunk.EstimatedTokens);

            var response = await _httpClient.PostAsync("/embed", content, cancellationToken);

            if (response.StatusCode == (System.Net.HttpStatusCode)413)
            {
                _logger.LogWarning("Chunk {ChunkId} too large for embedding service: {Length} chars, ~{Tokens} tokens. Consider reducing chunk size.",
                    chunk.Id, chunk.Content.Length, chunk.EstimatedTokens);
                return EmbeddingResult.CreateFailure($"Payload too large: {chunk.Content.Length} characters");
            }

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Embedding service response for chunk {ChunkId}: {ResponseLength} chars, status: {StatusCode}, starts with: {ResponseStart}",
                chunk.Id, responseContent.Length, response.StatusCode, responseContent.Length > 50 ? responseContent.Substring(0, 50) : responseContent);

            // Response is directly an array of floats, not wrapped in an object
            var embeddings = JsonSerializer.Deserialize<float[][]>(responseContent);

            if (embeddings?.Length > 0 && embeddings[0]?.Length > 0)
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("Successfully generated embedding for chunk {ChunkId} in {Duration}ms: vector dimension {Dimension}",
                    chunk.Id, duration.TotalMilliseconds, embeddings[0].Length);

                return EmbeddingResult.CreateSuccess(
                    embeddings[0], // First (and only) embedding
                    ModelName,
                    chunk.EstimatedTokens,
                    duration);
            }
            return EmbeddingResult.CreateFailure("No embeddings returned from service");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while generating embedding for chunk {ChunkId}", chunk.Id);
            return EmbeddingResult.CreateFailure($"HTTP error: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout while generating embedding for chunk {ChunkId}", chunk.Id);
            return EmbeddingResult.CreateFailure("Request timeout");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error while generating embedding for chunk {ChunkId}", chunk.Id);
            return EmbeddingResult.CreateFailure($"JSON error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while generating embedding for chunk {ChunkId}", chunk.Id);
            return EmbeddingResult.CreateFailure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<IList<EmbeddingResult>> GenerateBatchEmbeddingsAsync(IList<TextChunk> chunks, CancellationToken cancellationToken = default)
    {
        var results = new List<EmbeddingResult>();

        // For now, process sequentially to avoid overwhelming the service
        // TODO: Implement true batch processing if the service supports it
        foreach (var chunk in chunks)
        {
            var result = await GenerateEmbeddingAsync(chunk, cancellationToken);
            results.Add(result);

            // Add small delay between requests to be respectful
            await Task.Delay(50, cancellationToken);
        }

        return results;
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var healthCheck = new
            {
                inputs = "test",
                parameters = new { normalize = true }
            };

            var json = JsonSerializer.Serialize(healthCheck);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.PostAsync("/embeddings", content, cts.Token);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Embedding service health check failed");
            return false;
        }
    }

    private class EmbeddingResponse
    {
        public float[]? Embeddings { get; set; }
        public string? Error { get; set; }
    }
}
