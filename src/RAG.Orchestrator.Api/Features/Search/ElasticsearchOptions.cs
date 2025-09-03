namespace RAG.Orchestrator.Api.Features.Search;

public class ElasticsearchOptions
{
    public const string SectionName = "Services:Elasticsearch";
    
    public string Url { get; set; } = "http://localhost:9200";
    public string Username { get; set; } = "elastic";
    public string Password { get; set; } = "elastic";
    public int TimeoutMinutes { get; set; } = 10;
    public string DefaultIndexName { get; set; } = "rag-chunks";
    public bool AutoCreateIndices { get; set; } = true;
}
