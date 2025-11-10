using RAG.Orchestrator.Api.Models;

namespace RAG.Orchestrator.Api.Features.Settings;

public interface ISettingsService
{
    Task<LlmSettings?> GetLlmSettingsAsync();
    Task SetLlmSettingsAsync(LlmSettings settings);
    Task<ForumSettings?> GetForumSettingsAsync();
    Task SetForumSettingsAsync(ForumSettings settings);
}