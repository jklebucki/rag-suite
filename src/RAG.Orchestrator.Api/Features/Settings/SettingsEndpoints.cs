using RAG.Orchestrator.Api.Models;
using RAG.Orchestrator.Api.Services;

namespace RAG.Orchestrator.Api.Features.Settings;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings").WithTags("Settings");

        group.MapGet("/llm", async (ISettingsService service) =>
        {
            var settings = await service.GetLlmSettingsAsync();
            if (settings == null)
            {
                return Results.NotFound(new { Message = "LLM settings not found" });
            }

            var response = new LlmSettingsResponse
            {
                Url = settings.Url,
                MaxTokens = settings.MaxTokens,
                Temperature = settings.Temperature,
                Model = settings.Model,
                IsOllama = settings.IsOllama,
                TimeoutMinutes = settings.TimeoutMinutes,
                ChatEndpoint = settings.ChatEndpoint,
                GenerateEndpoint = settings.GenerateEndpoint
            };

            return Results.Ok(response);
        })
        .WithName("GetLlmSettings")
        .WithSummary("Get LLM settings")
        .WithDescription("Retrieves the current LLM service configuration settings.");

        group.MapPut("/llm", async (LlmSettingsRequest request, ISettingsService service) =>
        {
            var settings = new LlmSettings
            {
                Url = request.Url,
                MaxTokens = request.MaxTokens,
                Temperature = request.Temperature,
                Model = request.Model,
                IsOllama = request.IsOllama,
                TimeoutMinutes = request.TimeoutMinutes,
                ChatEndpoint = request.ChatEndpoint,
                GenerateEndpoint = request.GenerateEndpoint
            };

            await service.SetLlmSettingsAsync(settings);

            return Results.Ok(new { Message = "LLM settings updated successfully" });
        })
        .WithName("UpdateLlmSettings")
        .WithSummary("Update LLM settings")
        .WithDescription("Updates the LLM service configuration settings.");

        group.MapGet("/llm/models", async (string url, bool isOllama, ILlmService llmService) =>
        {
            try
            {
                var models = await llmService.GetAvailableModelsAsync(url, isOllama);
                return Results.Ok(new { Models = models });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to retrieve available models: {ex.Message}", statusCode: 500);
            }
        })
        .WithName("GetAvailableLlmModels")
        .WithSummary("Get available LLM models")
        .WithDescription("Retrieves the list of available models from the specified LLM service URL.");

        return app;
    }
}