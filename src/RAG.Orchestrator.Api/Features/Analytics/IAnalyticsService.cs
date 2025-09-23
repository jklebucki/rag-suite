using System.Threading;
using System.Threading.Tasks;

namespace RAG.Orchestrator.Api.Features.Analytics;

public interface IAnalyticsService
{
    Task<UsageStats> GetUsageStatsAsync(AnalyticsFilter? filter = null, CancellationToken cancellationToken = default);
    Task<PerformanceMetrics[]> GetPerformanceMetricsAsync(AnalyticsFilter? filter = null, CancellationToken cancellationToken = default);
    Task<SystemHealth> GetSystemHealthAsync(CancellationToken cancellationToken = default);
    Task<ElasticsearchStats> GetElasticsearchStatsAsync(CancellationToken cancellationToken = default);
    Task<IndexStats[]> GetIndexStatsAsync(string? indexName = null, CancellationToken cancellationToken = default);
    Task<NodeStats[]> GetNodeStatsAsync(CancellationToken cancellationToken = default);
    Task<SearchStatistics> GetSearchStatisticsAsync(CancellationToken cancellationToken = default);
}