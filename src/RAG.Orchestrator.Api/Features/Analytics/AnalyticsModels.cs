namespace RAG.Orchestrator.Api.Features.Analytics;

public record UsageStats(
    int TotalQueries,
    int TotalSessions,
    double AvgResponseTime,
    string[] TopQueries,
    Dictionary<string, int> PluginUsage
);

public record PerformanceMetrics(
    DateTime Timestamp,
    double ResponseTime,
    int ActiveSessions,
    string Endpoint
);

public record ElasticsearchStats(
    string ClusterName,
    string Status,
    int NumberOfNodes,
    int NumberOfDataNodes,
    int ActivePrimaryShards,
    int ActiveShards,
    int UnassignedShards,
    double ActiveShardsPercent
);

public record IndexStats(
    string IndexName,
    string Health,
    string Status,
    int DocumentCount,
    int DeletedDocuments,
    string StoreSize,
    long StoreSizeBytes,
    long IndexTotal,
    long IndexTimeInMillis,
    long SearchTotal,
    long SearchTimeInMillis,
    long GetTotal,
    long GetTimeInMillis
);

public record NodeStats(
    string NodeName,
    string NodeId,
    string[] Roles,
    long JvmMemoryUsed,
    long JvmMemoryMax,
    double JvmMemoryPercent,
    long DocumentCount,
    long IndexingCurrent,
    long SearchCurrent
);

public record SearchStatistics(
    long TotalSearches,
    long TotalSearchTimeMs,
    double AverageSearchTime,
    long SearchesLast24h,
    string MostActiveIndex,
    Dictionary<string, long> SearchesByIndex
);

public record SystemHealth(
    bool ElasticsearchAvailable,
    bool EmbeddingServiceAvailable,
    bool LlmServiceAvailable,
    ElasticsearchStats? ElasticsearchStats,
    IndexStats[] Indices,
    NodeStats[] Nodes,
    SearchStatistics SearchStats
);

public record AnalyticsFilter(
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? Endpoint = null,
    string? IndexName = null,
    bool IncludeSystemHealth = false,
    bool IncludeDetailedStats = false
);
