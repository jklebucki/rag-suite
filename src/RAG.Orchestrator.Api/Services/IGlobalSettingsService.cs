using RAG.Orchestrator.Api.Models;

namespace RAG.Orchestrator.Api.Services;

public interface IGlobalSettingsService
{
    Task<LlmSettings?> GetLlmSettingsAsync();
    Task SetLlmSettingsAsync(LlmSettings settings);
    Task InitializeLlmSettingsAsync(IConfiguration configuration);
}