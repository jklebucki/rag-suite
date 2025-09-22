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

    public async Task SetLlmSettingsAsync(LlmSettings settings)
    {
        await _globalSettingsService.SetLlmSettingsAsync(settings);
    }
}