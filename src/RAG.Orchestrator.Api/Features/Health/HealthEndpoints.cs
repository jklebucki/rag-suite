using RAG.Orchestrator.Api.Features.Health;
using RAG.Orchestrator.Api.Models;

namespace RAG.Orchestrator.Api.Features.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/healthz").WithTags("Health");

        group.MapGet("/system", async (IHealthAggregator aggregator) =>
        {
            var health = await aggregator.GetSystemHealthAsync();
            return health.ToApiResponse();
        })
        .WithName("SystemHealth")
        .WithSummary("Aggregated system health status")
        .WithDescription("Returns health information for API, LLM service, Elasticsearch, and vector store.");

        return app;
    }
}
