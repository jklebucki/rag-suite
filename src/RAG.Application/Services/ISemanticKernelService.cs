using Microsoft.SemanticKernel;
using RAG.Domain.ValueObjects;

namespace RAG.Application.Services;

/// <summary>
/// Main interface for Semantic Kernel orchestration following clean architecture
/// </summary>
public interface ISemanticKernelService
{
    /// <summary>
    /// Generate AI response with RAG context
    /// </summary>
    Task<RAGResponse> GenerateResponseAsync(string query, string sessionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Search documents using semantic search
    /// </summary>
    Task<IEnumerable<SearchResultDto>> SearchDocumentsAsync(SearchQuery query, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute business process plugin
    /// </summary>
    Task<string> ExecuteBusinessProcessQueryAsync(string query, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute Oracle query plugin
    /// </summary>
    Task<string> ExecuteOracleQueryAsync(string query, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get available plugins
    /// </summary>
    Task<IEnumerable<PluginInfo>> GetAvailablePluginsAsync();
}

public record RAGResponse
{
    public string Content { get; init; } = string.Empty;
    public IEnumerable<SourceReference> Sources { get; init; } = Array.Empty<SourceReference>();
    public string Model { get; init; } = string.Empty;
    public double ConfidenceScore { get; init; }
    public TimeSpan ProcessingTime { get; init; }
}

public record SourceReference
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Excerpt { get; init; } = string.Empty;
    public double Relevance { get; init; }
    public string Source { get; init; } = string.Empty;
}

public record SearchResultDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public double Score { get; init; }
    public string Category { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record PluginInfo
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IEnumerable<PluginFunction> Functions { get; init; } = Array.Empty<PluginFunction>();
}

public record PluginFunction
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IEnumerable<PluginParameter> Parameters { get; init; } = Array.Empty<PluginParameter>();
}

public record PluginParameter
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
}
