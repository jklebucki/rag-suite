using RAG.Orchestrator.Api.Features.Analytics;

namespace RAG.Orchestrator.Api.Features.Analytics;

public interface IAnalyticsService
{
    Task<UsageStats> GetUsageStatsAsync(AnalyticsFilter? filter = null, CancellationToken cancellationToken = default);
    Task<PerformanceMetrics[]> GetPerformanceMetricsAsync(AnalyticsFilter? filter = null, CancellationToken cancellationToken = default);
}

public class AnalyticsService : IAnalyticsService
{
    public Task<UsageStats> GetUsageStatsAsync(AnalyticsFilter? filter = null, CancellationToken cancellationToken = default)
    {
        // Mock data - replace with actual implementation
        var stats = new UsageStats(
            TotalQueries: 1247,
            TotalSessions: 89,
            AvgResponseTime: 1250.5,
            TopQueries: new[] { "Oracle schema", "user management", "process automation", "database backup", "IFS configuration" },
            PluginUsage: new Dictionary<string, int>
            {
                {"oracle-sql", 456},
                {"ifs-sop", 321},
                {"biz-process", 89}
            }
        );
        
        return Task.FromResult(stats);
    }

    public Task<PerformanceMetrics[]> GetPerformanceMetricsAsync(AnalyticsFilter? filter = null, CancellationToken cancellationToken = default)
    {
        // Mock data - replace with actual implementation
        var metrics = new PerformanceMetrics[]
        {
            new(DateTime.Now.AddMinutes(-10), 1200.5, 15, "/api/search"),
            new(DateTime.Now.AddMinutes(-8), 890.2, 12, "/api/chat/sessions"),
            new(DateTime.Now.AddMinutes(-5), 1450.8, 18, "/api/search"),
            new(DateTime.Now.AddMinutes(-2), 750.3, 10, "/api/plugins")
        };

        return Task.FromResult(metrics);
    }
}
