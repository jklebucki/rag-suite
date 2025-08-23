using RAG.Orchestrator.Api.Features.Search;

namespace RAG.Orchestrator.Api.Features.Search;

public interface ISearchService
{
    Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
}

public class SearchService : ISearchService
{
    public Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        // Mock data - replace with actual implementation
        var results = new SearchResult[]
        {
            new("1", "Oracle Database Schema Guide", 
                "Comprehensive guide to Oracle database schema design and best practices for RAG applications...", 
                0.95, "oracle", "schema", 
                new Dictionary<string, object> { {"table_count", 15}, {"size_mb", 250} }, 
                DateTime.Now.AddDays(-10), DateTime.Now.AddDays(-5)),
            
            new("2", "IFS SOP Document - User Management", 
                "Standard Operating Procedure for user management within IFS system including role assignments...", 
                0.87, "ifs", "sop", 
                new Dictionary<string, object> { {"version", "2.1"}, {"department", "IT"} }, 
                DateTime.Now.AddDays(-15), DateTime.Now.AddDays(-2)),
            
            new("3", "Business Process Automation Guidelines", 
                "Guidelines for implementing business process automation using workflow engines and approval systems...", 
                0.82, "files", "process", 
                new Dictionary<string, object> { {"category", "automation"}, {"complexity", "medium"} }, 
                DateTime.Now.AddDays(-7), DateTime.Now.AddDays(-1))
        };

        var filteredResults = results.Where(r => 
            r.Title.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ||
            r.Content.Contains(request.Query, StringComparison.OrdinalIgnoreCase)
        ).ToArray();

        var response = new SearchResponse(filteredResults, filteredResults.Length, 45, request.Query);
        return Task.FromResult(response);
    }
}
