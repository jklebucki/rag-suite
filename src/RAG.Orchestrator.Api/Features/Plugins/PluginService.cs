namespace RAG.Orchestrator.Api.Features.Plugins;

public class PluginService : IPluginService
{
    private static readonly List<PluginInfo> _plugins = new()
    {
        new("oracle-sql", "Oracle SQL Plugin", "Provides access to Oracle database schema and query capabilities", "1.0.0", true, new[] {"database", "sql", "schema"}),
        new("ifs-sop", "IFS SOP Plugin", "Handles IFS Standard Operating Procedures and documentation", "1.2.1", true, new[] {"sop", "documentation", "ifs"}),
        new("biz-process", "Business Process Plugin", "Manages business process workflows and automation", "2.0.0", false, new[] {"workflow", "automation", "process"})
    };

    public Task<PluginInfo[]> GetPluginsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_plugins.ToArray());
    }

    public Task<PluginInfo?> GetPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        var plugin = _plugins.FirstOrDefault(p => p.Id == pluginId);
        return Task.FromResult(plugin);
    }

    public Task<bool> EnablePluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        var index = _plugins.FindIndex(p => p.Id == pluginId);
        if (index == -1) return Task.FromResult(false);

        _plugins[index] = _plugins[index] with { Enabled = true };
        return Task.FromResult(true);
    }

    public Task<bool> DisablePluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        var index = _plugins.FindIndex(p => p.Id == pluginId);
        if (index == -1) return Task.FromResult(false);

        _plugins[index] = _plugins[index] with { Enabled = false };
        return Task.FromResult(true);
    }
}
