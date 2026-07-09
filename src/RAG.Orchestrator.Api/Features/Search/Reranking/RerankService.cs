using System.Text;
using System.Text.Json;

namespace RAG.Orchestrator.Api.Features.Search.Reranking;

/// <summary>
/// Cross-encoder reranker over an on-prem HTTP endpoint. Two API flavors are supported so the
/// serving backend can be chosen freely (no external/cloud calls in either case):
///
///   Api = "tei"    -> Text-Embeddings-Inference: POST /rerank {query, texts}       -> [{index, score}]
///   Api = "cohere" -> Infinity / Cohere-style:   POST /rerank {model, query, documents}
///                     -> {results: [{index, relevance_score}]}
///
/// Configuration (appsettings "Services:RerankService"):
///   Url            - base URL of the reranker service (required to enable)
///   Enabled        - master on/off switch (default: true when Url is set)
///   Api            - "tei" (default) or "cohere"
///   Model          - model id, sent in the request body for the "cohere" flavor
///   RetrieveTopN   - candidates fetched before reranking (default: 40)
///   TimeoutSeconds - request timeout (default: 30)
/// </summary>
public class RerankService : IRerankService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RerankService> _logger;
    private readonly bool _enabled;
    private readonly bool _cohereApi;
    private readonly string? _model;
    private readonly string _endpoint;
    private readonly int _timeoutSeconds;

    public RerankService(HttpClient httpClient, IConfiguration configuration, ILogger<RerankService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var section = configuration.GetSection("Services:RerankService");
        var url = section["Url"];
        var enabledSetting = section.GetValue<bool?>("Enabled");
        RetrieveTopN = section.GetValue<int?>("RetrieveTopN") ?? 40;
        _timeoutSeconds = section.GetValue<int?>("TimeoutSeconds") ?? 30;
        _cohereApi = string.Equals(section["Api"], "cohere", StringComparison.OrdinalIgnoreCase);
        _model = section["Model"];

        _enabled = !string.IsNullOrWhiteSpace(url) && (enabledSetting ?? true);

        if (_enabled)
        {
            var baseUri = new Uri(url!);
            _httpClient.BaseAddress = baseUri;
            _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
            _endpoint = new Uri(baseUri, "/rerank").ToString();
        }
        else
        {
            _endpoint = string.Empty;
        }
    }

    private string ApiName => _cohereApi ? "cohere" : "tei";

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

        _logger.LogInformation(
            "Reranking {Count} candidates via {Endpoint} (api={Api}, model={Model}, timeout={Timeout}s)",
            documents.Count, _endpoint, ApiName, string.IsNullOrWhiteSpace(_model) ? "(none)" : _model, _timeoutSeconds);

        try
        {
            var json = JsonSerializer.Serialize(BuildRequestPayload(query, documents));
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/rerank", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Reranker {Endpoint} returned {StatusCode}; falling back to original order",
                    _endpoint, (int)response.StatusCode);
                return Array.Empty<RerankHit>();
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var hits = ParseHits(body);
            if (hits.Count == 0)
            {
                _logger.LogWarning("Reranker {Endpoint} returned no parseable results; falling back to original order",
                    _endpoint);
            }

            return hits;
        }
        catch (Exception ex)
        {
            // Reranking is a best-effort quality boost: never fail the search because of it.
            _logger.LogWarning(ex, "Reranking via {Endpoint} failed; falling back to original retrieval order", _endpoint);
            return Array.Empty<RerankHit>();
        }
    }

    private Dictionary<string, object> BuildRequestPayload(string query, IReadOnlyList<string> documents)
    {
        if (_cohereApi)
        {
            // Infinity / Cohere-compatible rerank API.
            var payload = new Dictionary<string, object>
            {
                ["query"] = query,
                ["documents"] = documents,
                ["return_documents"] = false
            };
            if (!string.IsNullOrWhiteSpace(_model))
            {
                payload["model"] = _model!;
            }
            return payload;
        }

        // Text-Embeddings-Inference rerank API.
        return new Dictionary<string, object>
        {
            ["query"] = query,
            ["texts"] = documents,
            ["truncate"] = true
        };
    }

    /// <summary>
    /// Parses a /rerank response. Handles both TEI (a JSON array of {index, score}) and
    /// Infinity/Cohere ({results: [{index, relevance_score}]}). Returns hits sorted by descending score.
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

            if (!item.TryGetProperty("index", out var indexElement) || !indexElement.TryGetInt32(out var index))
            {
                continue;
            }

            // TEI uses "score"; Infinity/Cohere use "relevance_score".
            if ((item.TryGetProperty("score", out var scoreElement) && scoreElement.TryGetDouble(out var score)) ||
                (item.TryGetProperty("relevance_score", out scoreElement) && scoreElement.TryGetDouble(out score)))
            {
                hits.Add(new RerankHit(index, score));
            }
        }

        hits.Sort((a, b) => b.Score.CompareTo(a.Score));
        return hits;
    }
}
