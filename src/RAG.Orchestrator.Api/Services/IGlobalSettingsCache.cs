using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Models;

namespace RAG.Orchestrator.Api.Services;

/// <summary>
/// Interface for a singleton storing global settings in memory.
/// Provides thread-safe access to settings, especially for methods used in chat requests.
/// Settings update is two-phase: first in the singleton, then in the database.
/// </summary>
public interface IGlobalSettingsCache
{
    /// <summary>
    /// Initializes the cache by loading all settings from the database.
    /// Called during application startup.
    /// </summary>
    Task InitializeAsync(ChatDbContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves LLM settings in a thread-safe manner.
    /// Used by chat requests for concurrent access.
    /// </summary>
    Task<LlmSettings?> GetLlmSettingsAsync();

    /// <summary>
    /// Updates LLM settings in a two-phase manner:
    /// 1. First updates in the singleton (in memory).
    /// 2. On success, saves to the database.
    /// In case of database error, rolls back changes in the singleton.
    /// </summary>
    Task SetLlmSettingsAsync(LlmSettings settings, ChatDbContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves Forum settings.
    /// </summary>
    Task<ForumSettings?> GetForumSettingsAsync();

    /// <summary>
    /// Updates Forum settings with two-phase commit (cache + database).
    /// </summary>
    Task SetForumSettingsAsync(ForumSettings settings, ChatDbContext context, CancellationToken cancellationToken = default);
}