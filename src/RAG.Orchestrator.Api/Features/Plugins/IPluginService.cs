namespace RAG.Orchestrator.Api.Features.Plugins;

public interface IPluginService
{
    Task<PluginInfo[]> GetPluginsAsync(CancellationToken cancellationToken = default);
    Task<PluginInfo?> GetPluginAsync(string pluginId, CancellationToken cancellationToken = default);
    Task<bool> EnablePluginAsync(string pluginId, CancellationToken cancellationToken = default);
    Task<bool> DisablePluginAsync(string pluginId, CancellationToken cancellationToken = default);
}