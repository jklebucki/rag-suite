using Microsoft.SemanticKernel;
using System.ComponentModel;
using RAG.Application.Services;

namespace RAG.Application.Plugins;

/// <summary>
/// Business Process Plugin for Semantic Kernel - replaces empty BizProcessPlugin
/// </summary>
public class BusinessProcessPlugin
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<BusinessProcessPlugin> _logger;

    public BusinessProcessPlugin(IElasticsearchService elasticsearchService, ILogger<BusinessProcessPlugin> logger)
    {
        _elasticsearchService = elasticsearchService;
        _logger = logger;
    }

    [KernelFunction]
    [Description("Search business processes and procedures")]
    public async Task<string> SearchBusinessProcesses(
        [Description("The search query for business processes")] string query)
    {
        try
        {
            _logger.LogInformation("Searching business processes for: {Query}", query);

            var searchResults = await _elasticsearchService.SearchAsync(new SearchRequest
            {
                Query = query,
                Filters = new SearchFilters { Category = "business-process" },
                Limit = 5
            });

            if (!searchResults.Any())
            {
                return "No business processes found for the given query.";
            }

            var formattedResults = searchResults.Select(r => $"""
                **{r.Title}**
                {r.Content.Substring(0, Math.Min(200, r.Content.Length))}...
                (Relevance: {r.Score:F2})
                """).ToList();

            return $"""
                Found {searchResults.Count()} business processes:

                {string.Join("\n\n", formattedResults)}
                """;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching business processes");
            return "Error occurred while searching business processes.";
        }
    }

    [KernelFunction]
    [Description("Get specific business process by ID")]
    public async Task<string> GetBusinessProcessById(
        [Description("The ID of the business process")] string processId)
    {
        try
        {
            var process = await _elasticsearchService.GetDocumentByIdAsync(processId);
            
            return $"""
                **{process.Title}**
                
                {process.Content}
                
                Category: {process.Category}
                Created: {process.CreatedAt:yyyy-MM-dd}
                """;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business process {ProcessId}", processId);
            return $"Business process with ID {processId} not found.";
        }
    }

    [KernelFunction]
    [Description("List all available business process categories")]
    public async Task<string> GetBusinessProcessCategories()
    {
        try
        {
            var categories = await _elasticsearchService.GetCategoriesAsync("business-process");
            
            return $"""
                Available business process categories:
                
                {string.Join("\n", categories.Select(c => $"• {c}"))}
                """;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business process categories");
            return "Error occurred while retrieving categories.";
        }
    }
}

// Temporary interface - will be moved to proper layer
public interface IElasticsearchService
{
    Task<IEnumerable<SearchResultDto>> SearchAsync(SearchRequest request);
    Task<DocumentDetail> GetDocumentByIdAsync(string id);
    Task<IEnumerable<string>> GetCategoriesAsync(string type);
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public SearchFilters? Filters { get; set; }
    public int Limit { get; set; } = 10;
}

public class SearchFilters
{
    public string? Category { get; set; }
}

public class DocumentDetail
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
