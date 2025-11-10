using RAG.Orchestrator.Api.Models;
using RAG.Orchestrator.Api.Services;

namespace RAG.Orchestrator.Api.Features.Settings;

public class SettingsService : ISettingsService
{
    private readonly IGlobalSettingsService _globalSettingsService;

    public SettingsService(IGlobalSettingsService globalSettingsService)
    {
        _globalSettingsService = globalSettingsService;
    }

    public async Task<LlmSettings?> GetLlmSettingsAsync()
    {
        return await _globalSettingsService.GetLlmSettingsAsync();
    }

    public async Task<ForumSettings?> GetForumSettingsAsync()
    {
        return await _globalSettingsService.GetForumSettingsAsync();
    }

    public async Task SetLlmSettingsAsync(LlmSettings settings)
    {
        await _globalSettingsService.SetLlmSettingsAsync(settings);
    }

    public async Task SetForumSettingsAsync(ForumSettings settings)
    {
        await _globalSettingsService.SetForumSettingsAsync(settings);
    }
}