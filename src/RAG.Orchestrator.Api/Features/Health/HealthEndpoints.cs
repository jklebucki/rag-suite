using RAG.Orchestrator.Api.Common.Api;

namespace RAG.Orchestrator.Api.Features.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/healthz").WithTags("Health");

        group.MapGet("/system", async (IHealthAggregator aggregator) =>
        {
            try
            {
                var health = await aggregator.GetSystemHealthAsync();
                return health.ToApiResponse();
            }
            catch (Exception ex)
            {
                // Return error response instead of crashing
                var errorResponse = new
                {
                    Status = "Error",
                    Message = ex.Message,
                    Timestamp = DateTime.UtcNow,
                    Services = new
                    {
                        Api = "healthy", // if this code runs, API is up
                        Llm = "error",
                        Elasticsearch = "error",
                        VectorStore = "unknown"
                    }
                };
                return Results.Json(errorResponse, statusCode: 503);
            }
        })
        .WithName("SystemHealth")
        .WithSummary("Aggregated system health status")
        .WithDescription("Returns health information for API, LLM service, Elasticsearch, and vector store.");

        return app;
    }
}
