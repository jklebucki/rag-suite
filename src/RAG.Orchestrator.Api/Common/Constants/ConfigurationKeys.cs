namespace RAG.Orchestrator.Api.Common.Constants;

/// <summary>
/// Constants for configuration section keys
/// </summary>
public static class ConfigurationKeys
{
    /// <summary>
    /// Connection string key for SecurityDatabase
    /// </summary>
    public const string SecurityDatabaseConnectionString = "ConnectionStrings:SecurityDatabase";

    /// <summary>
    /// Elasticsearch configuration section
    /// </summary>
    public static class Elasticsearch
    {
        public const string Section = "Services:Elasticsearch";
        public const string Url = "Services:Elasticsearch:Url";
        public const string Username = "Services:Elasticsearch:Username";
        public const string Password = "Services:Elasticsearch:Password";
        public const string TimeoutMinutes = "Services:Elasticsearch:TimeoutMinutes";
    }

    /// <summary>
    /// Embedding service configuration section
    /// </summary>
    public static class EmbeddingService
    {
        public const string Section = "Services:EmbeddingService";
        public const string Url = "Services:EmbeddingService:Url";
    }

    /// <summary>
    /// LLM service configuration section
    /// </summary>
    public static class LlmService
    {
        public const string Section = "Services:LlmService";
        public const string Url = "Services:LlmService:Url";
        public const string IsOllama = "Services:LlmService:IsOllama";
    }

    /// <summary>
    /// Chat configuration section
    /// </summary>
    public static class Chat
    {
        public const string Section = "Chat";
        public const string MaxMessageLength = "Chat:MaxMessageLength";
    }

    /// <summary>
    /// Migrations configuration section
    /// </summary>
    public static class Migrations
    {
        public const string AutoApply = "Migrations:AutoApply";
    }
}

