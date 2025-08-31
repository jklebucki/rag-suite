using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RAG.Application.Plugins;
using RAG.Application.Services;

namespace RAG.Infrastructure.SemanticKernel;

/// <summary>
/// Semantic Kernel configuration following clean architecture principles
/// </summary>
public static class SemanticKernelConfiguration
{
    public static IServiceCollection AddSemanticKernel(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Kernel with Ollama
        services.AddKernel()
            .AddOllamaChatCompletion(
                modelId: configuration["Services:LlmService:Model"] ?? "llama3.1:8b-instruct-q8_0",
                endpoint: new Uri(configuration["Services:LlmService:Url"] ?? "http://localhost:11434"))
            .Services
            .AddSingleton<KernelPluginCollection>(serviceProvider =>
            {
                var pluginCollection = new KernelPluginCollection();
                
                // Register business process plugin
                var businessPlugin = KernelPluginFactory.CreateFromObject(
                    serviceProvider.GetRequiredService<BusinessProcessPlugin>(),
                    "BusinessProcessPlugin");
                pluginCollection.Add(businessPlugin);
                
                // Register Oracle plugin
                var oraclePlugin = KernelPluginFactory.CreateFromObject(
                    serviceProvider.GetRequiredService<OracleQueryPlugin>(),
                    "OracleQueryPlugin");
                pluginCollection.Add(oraclePlugin);
                
                return pluginCollection;
            });

        // Register custom services
        services.AddScoped<ISemanticKernelService, SemanticKernelService>();
        services.AddScoped<BusinessProcessPlugin>();
        services.AddScoped<OracleQueryPlugin>();
        
        return services;
    }
}

/// <summary>
/// Implementation of Semantic Kernel service with clean architecture
/// </summary>
public class SemanticKernelService : ISemanticKernelService
{
    private readonly Kernel _kernel;
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<SemanticKernelService> _logger;
    private readonly IConfiguration _configuration;

    public SemanticKernelService(
        Kernel kernel,
        IElasticsearchService elasticsearchService,
        ILogger<SemanticKernelService> logger,
        IConfiguration configuration)
    {
        _kernel = kernel;
        _elasticsearchService = elasticsearchService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<RAGResponse> GenerateResponseAsync(string query, string sessionId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Generating RAG response for query: {Query}", query);

            // Step 1: Search for relevant context
            var searchResults = await _elasticsearchService.SearchAsync(new SearchRequest
            {
                Query = query,
                Limit = 5
            });

            // Step 2: Build context-aware prompt
            var contextPrompt = BuildContextualPrompt(query, searchResults);

            // Step 3: Invoke Semantic Kernel
            var result = await _kernel.InvokePromptAsync(contextPrompt, cancellationToken: cancellationToken);

            var processingTime = DateTime.UtcNow - startTime;

            return new RAGResponse
            {
                Content = result.ToString(),
                Sources = searchResults.Select(r => new SourceReference
                {
                    Id = r.Id,
                    Title = r.Title,
                    Excerpt = r.Content.Length > 200 ? r.Content.Substring(0, 200) + "..." : r.Content,
                    Relevance = r.Score,
                    Source = "Elasticsearch"
                }),
                Model = _configuration["Services:LlmService:Model"] ?? "unknown",
                ConfidenceScore = CalculateConfidenceScore(searchResults),
                ProcessingTime = processingTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating RAG response for query: {Query}", query);
            throw;
        }
    }

    public async Task<IEnumerable<SearchResultDto>> SearchDocumentsAsync(
        Domain.ValueObjects.SearchQuery query, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var searchRequest = new SearchRequest
            {
                Query = query.Text,
                Limit = query.MaxResults,
                Filters = query.Filters != null ? new SearchFilters
                {
                    Category = query.Filters.Category
                } : null
            };

            var results = await _elasticsearchService.SearchAsync(searchRequest);
            
            return results.Where(r => r.Score >= query.MinRelevanceThreshold);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents");
            throw;
        }
    }

    public async Task<string> ExecuteBusinessProcessQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var businessPlugin = _kernel.Plugins["BusinessProcessPlugin"];
            var function = businessPlugin["SearchBusinessProcesses"];
            
            var result = await _kernel.InvokeAsync(function, new() { ["query"] = query }, cancellationToken);
            return result.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing business process query");
            throw;
        }
    }

    public async Task<string> ExecuteOracleQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var oraclePlugin = _kernel.Plugins["OracleQueryPlugin"];
            var function = oraclePlugin["ExecuteQuery"];
            
            var result = await _kernel.InvokeAsync(function, new() { ["sqlQuery"] = query }, cancellationToken);
            return result.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Oracle query");
            throw;
        }
    }

    public async Task<IEnumerable<PluginInfo>> GetAvailablePluginsAsync()
    {
        return await Task.FromResult(_kernel.Plugins.Select(plugin => new PluginInfo
        {
            Name = plugin.Name,
            Description = plugin.Description ?? "No description available",
            Functions = plugin.Select(function => new PluginFunction
            {
                Name = function.Name,
                Description = function.Description ?? "No description available",
                Parameters = function.Metadata.Parameters.Select(param => new PluginParameter
                {
                    Name = param.Name,
                    Type = param.ParameterType?.Name ?? "object",
                    Description = param.Description ?? "No description available",
                    IsRequired = param.IsRequired
                })
            })
        }));
    }

    private static string BuildContextualPrompt(string query, IEnumerable<SearchResultDto> searchResults)
    {
        var context = string.Join("\n", searchResults.Select(r => 
            $"Source: {r.Title}\nContent: {r.Content.Substring(0, Math.Min(300, r.Content.Length))}...\n"));

        return $"""
            You are an intelligent AI assistant for the RAG Suite system. Answer questions in Polish, professionally and helpfully.

            Context from knowledge base:
            {context}

            User question: {query}

            Instructions:
            1. Use the provided context to answer the question accurately
            2. If the context doesn't contain relevant information, say so clearly
            3. Provide citations to sources when possible
            4. Be concise but comprehensive
            5. Answer in Polish

            Answer:
            """;
    }

    private static double CalculateConfidenceScore(IEnumerable<SearchResultDto> searchResults)
    {
        if (!searchResults.Any()) return 0.0;
        
        var avgScore = searchResults.Average(r => r.Score);
        var count = searchResults.Count();
        
        // Simple confidence calculation based on average score and result count
        return Math.Min(1.0, avgScore * Math.Min(1.0, count / 3.0));
    }
}
