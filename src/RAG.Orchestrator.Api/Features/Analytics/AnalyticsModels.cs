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

public record AnalyticsFilter(
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? Endpoint = null
);
