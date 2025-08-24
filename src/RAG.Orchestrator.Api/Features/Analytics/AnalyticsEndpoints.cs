using RAG.Orchestrator.Api.Features.Analytics;
using RAG.Orchestrator.Api.Models;

namespace RAG.Orchestrator.Api.Features.Analytics;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/analytics")
            .WithTags("Analytics")
            .WithOpenApi();

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

        return endpoints;
    }
}
