using System.Text.Json.Serialization;

namespace RAG.Orchestrator.Api.Features.Embeddings.Models;

public class EmbeddingRequest
{
    [JsonPropertyName("inputs")]
    public string Inputs { get; set; } = string.Empty;
}

public class EmbeddingResponse
{
    [JsonPropertyName("embeddings")]
    public float[][] Embeddings { get; set; } = Array.Empty<float[]>();

    [JsonPropertyName("model")]
    public int Model { get; set; }
}
