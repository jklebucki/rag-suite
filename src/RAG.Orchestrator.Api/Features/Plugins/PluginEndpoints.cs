using RAG.Orchestrator.Api.Models;

namespace RAG.Orchestrator.Api.Features.Plugins;

public static class PluginEndpoints
{
    public static IEndpointRouteBuilder MapPluginEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/plugins")
            .WithTags("Plugins")
            .WithOpenApi();

        group.MapGet("/", async (IPluginService pluginService) =>
        {
            var plugins = await pluginService.GetPluginsAsync();
            return plugins.ToApiResponse();
        })
        .WithName("GetPlugins")
        .WithSummary("Get all plugins")
        .WithDescription("Retrieve all available plugins and their status");

        group.MapGet("/{pluginId}", async (string pluginId, IPluginService pluginService) =>
        {
            var plugin = await pluginService.GetPluginAsync(pluginId);
            return plugin != null ? plugin.ToApiResponse() : ApiResponseExtensions.ToApiNotFoundResponse<PluginInfo>();
        })
        .WithName("GetPlugin")
        .WithSummary("Get a specific plugin")
        .WithDescription("Retrieve information about a specific plugin by its ID");

        group.MapPost("/{pluginId}/enable", async (string pluginId, IPluginService pluginService) =>
        {
            var success = await pluginService.EnablePluginAsync(pluginId);
            return success ? Results.Ok() : Results.NotFound();
        })
        .WithName("EnablePlugin")
        .WithSummary("Enable a plugin")
        .WithDescription("Enable a plugin to make it available for use");

        group.MapPost("/{pluginId}/disable", async (string pluginId, IPluginService pluginService) =>
        {
            var success = await pluginService.DisablePluginAsync(pluginId);
            return success ? Results.Ok() : Results.NotFound();
        })
        .WithName("DisablePlugin")
        .WithSummary("Disable a plugin")
        .WithDescription("Disable a plugin to prevent it from being used");

        return endpoints;
    }
}
