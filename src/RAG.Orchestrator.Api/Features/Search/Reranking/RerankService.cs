using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RAG.Orchestrator.Api.Features.Search.Reranking;

/// <summary>
/// Cross-encoder reranker backed by a Text-Embeddings-Inference (TEI) "/rerank" endpoint
/// (e.g. serving BAAI/bge-reranker-v2-m3). Fully on-prem; no external API calls.
///
/// Configuration (appsettings "Services:RerankService"):
///   Url            - base URL of the reranker service (required to enable)
///   Enabled        - master on/off switch (default: true when Url is set)
///   RetrieveTopN   - candidates fetched before reranking (default: 40)
///   TimeoutSeconds - request timeout (default: 30)
/// </summary>
public class RerankService : IRerankService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RerankService> _logger;
    private readonly bool _enabled;

    public RerankService(HttpClient httpClient, IConfiguration configuration, ILogger<RerankService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var section = configuration.GetSection("Services:RerankService");
        var url = section["Url"];
        var enabledSetting = section.GetValue<bool?>("Enabled");
        RetrieveTopN = section.GetValue<int?>("RetrieveTopN") ?? 40;
        var timeoutSeconds = section.GetValue<int?>("TimeoutSeconds") ?? 30;

        _enabled = !string.IsNullOrWhiteSpace(url) && (enabledSetting ?? true);

        if (_enabled)
        {
            _httpClient.BaseAddress = new Uri(url!);
            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        }
    }

    public bool IsEnabled => _enabled;

    public int RetrieveTopN { get; }

    public async Task<IReadOnlyList<RerankHit>> RerankAsync(
        string query,
        IReadOnlyList<string> documents,
        CancellationToken cancellationToken = default)
    {
        if (!_enabled || documents.Count == 0)
        {
            return Array.Empty<RerankHit>();
        }

        try
        {
            var payload = new RerankRequest
            {
                Query = query,
                Texts = documents.ToArray(),
                Truncate = true
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/rerank", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Reranker returned {StatusCode}; falling back to original order", response.StatusCode);
                return Array.Empty<RerankHit>();
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var hits = ParseHits(body);
            if (hits.Count == 0)
            {
                _logger.LogWarning("Reranker returned no parseable results; falling back to original order");
            }

            return hits;
        }
        catch (Exception ex)
        {
            // Reranking is a best-effort quality boost: never fail the search because of it.
            _logger.LogWarning(ex, "Reranking failed; falling back to original retrieval order");
            return Array.Empty<RerankHit>();
        }
    }

    /// <summary>
    /// Parses the TEI /rerank response, which is a JSON array of {index, score} objects
    /// (optionally wrapped in a "results" property). Returns hits sorted by descending score.
    /// </summary>
    private static List<RerankHit> ParseHits(string body)
    {
        var hits = new List<RerankHit>();
        if (string.IsNullOrWhiteSpace(body))
        {
            return hits;
        }

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("results", out var resultsElement))
        {
            root = resultsElement;
        }

        if (root.ValueKind != JsonValueKind.Array)
        {
            return hits;
        }

        foreach (var item in root.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (item.TryGetProperty("index", out var indexElement) && indexElement.TryGetInt32(out var index) &&
                item.TryGetProperty("score", out var scoreElement) && scoreElement.TryGetDouble(out var score))
            {
                hits.Add(new RerankHit(index, score));
            }
        }

        hits.Sort((a, b) => b.Score.CompareTo(a.Score));
        return hits;
    }

    private sealed class RerankRequest
    {
        [JsonPropertyName("query")]
        public string Query { get; init; } = string.Empty;

        [JsonPropertyName("texts")]
        public string[] Texts { get; init; } = Array.Empty<string>();

        [JsonPropertyName("truncate")]
        public bool Truncate { get; init; } = true;
    }
}
