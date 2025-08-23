namespace RAG.Orchestrator.Api.Features.Plugins;

public record PluginInfo(
    string Id,
    string Name,
    string Description,
    string Version,
    bool Enabled,
    string[] Capabilities
);
