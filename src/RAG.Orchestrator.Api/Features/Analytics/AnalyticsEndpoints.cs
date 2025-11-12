using RAG.Abstractions.Common.Api;
using RAG.Orchestrator.Api.Common.Api;

namespace RAG.Orchestrator.Api.Features.Analytics;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/analytics")
            .WithTags("Analytics")
            .WithOpenApi();

        // Usage Statistics
        group.MapGet("/usage", async (IAnalyticsService analyticsService,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? endpoint = null) =>
        {
            var filter = new AnalyticsFilter(startDate, endDate, endpoint);
            var stats = await analyticsService.GetUsageStatsAsync(filter);
            return stats.ToApiResponse();
        })
        .WithName("GetUsageStats")
        .WithSummary("Get usage statistics")
        .WithDescription("Retrieve usage statistics including total queries, sessions, and plugin usage");

        // Performance Metrics
        group.MapGet("/performance", async (IAnalyticsService analyticsService,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? endpoint = null) =>
        {
            var filter = new AnalyticsFilter(startDate, endDate, endpoint);
            var metrics = await analyticsService.GetPerformanceMetricsAsync(filter);
            return metrics.ToApiResponse();
        })
        .WithName("GetPerformanceMetrics")
        .WithSummary("Get performance metrics")
        .WithDescription("Retrieve performance metrics including response times and active sessions");

        // System Health Overview
        group.MapGet("/health", async (IAnalyticsService analyticsService) =>
        {
            var health = await analyticsService.GetSystemHealthAsync();
            return health.ToApiResponse();
        })
        .WithName("GetSystemHealth")
        .WithSummary("Get system health status")
        .WithDescription("Retrieve comprehensive system health including all services and components");

        // Elasticsearch Cluster Statistics
        group.MapGet("/elasticsearch/cluster", async (IAnalyticsService analyticsService) =>
        {
            var stats = await analyticsService.GetElasticsearchStatsAsync();
            return stats.ToApiResponse();
        })
        .WithName("GetElasticsearchClusterStats")
        .WithSummary("Get Elasticsearch cluster statistics")
        .WithDescription("Retrieve detailed Elasticsearch cluster health and statistics");

        // Index Statistics
        group.MapGet("/elasticsearch/indices", async (IAnalyticsService analyticsService,
            string? indexName = null) =>
        {
            var stats = await analyticsService.GetIndexStatsAsync(indexName);
            return stats.ToApiResponse();
        })
        .WithName("GetIndexStats")
        .WithSummary("Get index statistics")
        .WithDescription("Retrieve statistics for all indices or a specific index");

        // Specific Index Statistics
        group.MapGet("/elasticsearch/indices/{indexName}", async (string indexName, IAnalyticsService analyticsService) =>
        {
            var stats = await analyticsService.GetIndexStatsAsync(indexName);
            var specificIndex = stats.FirstOrDefault(s => s.IndexName == indexName);
            return specificIndex?.ToApiResponse() ?? Results.NotFound($"Index '{indexName}' not found");
        })
        .WithName("GetSpecificIndexStats")
        .WithSummary("Get specific index statistics")
        .WithDescription("Retrieve detailed statistics for a specific index");

        // Node Statistics
        group.MapGet("/elasticsearch/nodes", async (IAnalyticsService analyticsService) =>
        {
            var stats = await analyticsService.GetNodeStatsAsync();
            return stats.ToApiResponse();
        })
        .WithName("GetNodeStats")
        .WithSummary("Get Elasticsearch node statistics")
        .WithDescription("Retrieve detailed statistics for all Elasticsearch nodes");

        // Search Statistics
        group.MapGet("/search", async (IAnalyticsService analyticsService) =>
        {
            var stats = await analyticsService.GetSearchStatisticsAsync();
            return stats.ToApiResponse();
        })
        .WithName("GetSearchStatistics")
        .WithSummary("Get search statistics")
        .WithDescription("Retrieve comprehensive search performance and usage statistics");

        // Combined Dashboard Data
        group.MapGet("/dashboard", async (IAnalyticsService analyticsService,
            bool includeDetailedStats = false) =>
        {
            var filter = new AnalyticsFilter(IncludeDetailedStats: includeDetailedStats, IncludeSystemHealth: true);

            var usageTask = analyticsService.GetUsageStatsAsync(filter);
            var performanceTask = analyticsService.GetPerformanceMetricsAsync(filter);
            var healthTask = analyticsService.GetSystemHealthAsync();
            var searchStatsTask = analyticsService.GetSearchStatisticsAsync();

            await Task.WhenAll(usageTask, performanceTask, healthTask, searchStatsTask);

            var dashboardData = new
            {
                Usage = await usageTask,
                Performance = await performanceTask,
                Health = await healthTask,
                SearchStats = await searchStatsTask,
                Timestamp = DateTime.UtcNow
            };

            return dashboardData.ToApiResponse();
        })
        .WithName("GetDashboardData")
        .WithSummary("Get complete dashboard data")
        .WithDescription("Retrieve all analytics data in a single request for dashboard display");

        // Real-time Status Check
        group.MapGet("/status", async (IAnalyticsService analyticsService) =>
        {
            var health = await analyticsService.GetSystemHealthAsync();
            var searchStats = await analyticsService.GetSearchStatisticsAsync();

            var status = new
            {
                Status = health.ElasticsearchAvailable && health.EmbeddingServiceAvailable ? "Healthy" : "Degraded",
                Services = new
                {
                    Elasticsearch = health.ElasticsearchAvailable ? "Online" : "Offline",
                    EmbeddingService = health.EmbeddingServiceAvailable ? "Online" : "Offline",
                    LlmService = health.LlmServiceAvailable ? "Online" : "Offline"
                },
                QuickStats = new
                {
                    TotalSearches = searchStats.TotalSearches,
                    AverageSearchTime = Math.Round(searchStats.AverageSearchTime, 2),
                    ActiveIndices = health.Indices.Length,
                    ActiveNodes = health.Nodes.Length
                },
                LastUpdated = DateTime.UtcNow
            };

            return status.ToApiResponse();
        })
        .WithName("GetQuickStatus")
        .WithSummary("Get quick system status")
        .WithDescription("Retrieve essential system status information for monitoring");

        return endpoints;
    }
}
