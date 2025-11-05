namespace RAG.Orchestrator.Api.Common.Constants;

/// <summary>
/// Constants for API endpoint paths
/// </summary>
public static class ApiEndpoints
{
    /// <summary>
    /// Ollama API endpoints
    /// </summary>
    public static class Ollama
    {
        public const string Generate = "/api/generate";
        public const string Chat = "/api/chat";
        public const string Tags = "/api/tags";
    }

    /// <summary>
    /// TGI API endpoints
    /// </summary>
    public static class Tgi
    {
        public const string Generate = "/generate";
        public const string Health = "/health";
    }

    /// <summary>
    /// Elasticsearch API endpoints
    /// </summary>
    public static class Elasticsearch
    {
        public const string Root = "/";
        public const string ClusterHealth = "/_cluster/health";
        public const string NodesStats = "/_nodes/stats";
        public const string Stats = "/_stats";
        public const string Search = "/_search";
        public const string Document = "/_doc";
    }
}

