using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Models;

namespace RAG.Orchestrator.Api.Services;

public interface IGlobalSettingsService
{
    Task<LlmSettings?> GetLlmSettingsAsync();
    Task SetLlmSettingsAsync(LlmSettings settings);
    Task InitializeLlmSettingsAsync(IConfiguration configuration, ChatDbContext context);
    Task<ForumSettings?> GetForumSettingsAsync();
    Task SetForumSettingsAsync(ForumSettings settings);
    Task InitializeForumSettingsAsync(IConfiguration configuration, ChatDbContext context);
}