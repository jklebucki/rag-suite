using Microsoft.EntityFrameworkCore;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace RAG.Orchestrator.Api.Services;

/// <summary>
/// Singleton storing global settings in memory.
/// Loads data from the database on startup and ensures thread-safe access.
/// Update is two-phase: first in memory, then in database with rollback handling.
/// </summary>
public class GlobalSettingsCache : IGlobalSettingsCache
{
    private readonly ConcurrentDictionary<string, GlobalSetting> _settings = new();
    private readonly object _lock = new(); // For complex operations requiring synchronization

    /// <summary>
    /// Initializes the cache by loading all settings from the database.
    /// </summary>
    public async Task InitializeAsync(ChatDbContext context, CancellationToken cancellationToken = default)
    {
        var settingsFromDb = await context.GlobalSettings.ToListAsync(cancellationToken);

        lock (_lock)
        {
            _settings.Clear();
            foreach (var setting in settingsFromDb)
            {
                _settings[setting.Key] = setting;
            }
        }
    }

    /// <summary>
    /// Retrieves LLM settings in a thread-safe manner.
    /// </summary>
    public Task<LlmSettings?> GetLlmSettingsAsync()
    {
        if (_settings.TryGetValue(GlobalSettingKeys.LlmService, out var setting) && !string.IsNullOrEmpty(setting.Value))
        {
            try
            {
                return Task.FromResult(JsonSerializer.Deserialize<LlmSettings>(setting.Value));
            }
            catch
            {
                return Task.FromResult<LlmSettings?>(null);
            }
        }

        return Task.FromResult<LlmSettings?>(null);
    }

    /// <summary>
    /// Updates LLM settings in a two-phase manner.
    /// </summary>
    public async Task SetLlmSettingsAsync(LlmSettings settings, ChatDbContext context, CancellationToken cancellationToken = default)
    {
        var jsonValue = JsonSerializer.Serialize(settings);
        var key = GlobalSettingKeys.LlmService;

        // Phase 1: Update in memory (with memory of previous value for rollback)
        GlobalSetting? previousSetting = null;
        lock (_lock)
        {
            if (_settings.TryGetValue(key, out var existing))
            {
                previousSetting = new GlobalSetting
                {
                    Id = existing.Id,
                    Key = existing.Key,
                    Value = existing.Value
                };
            }

            var newSetting = new GlobalSetting
            {
                Id = previousSetting?.Id ?? 0, // Id will be set by EF on save
                Key = key,
                Value = jsonValue
            };
            _settings[key] = newSetting;
        }

        // Phase 2: Save to database
        try
        {
            var dbSetting = await context.GlobalSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
            if (dbSetting == null)
            {
                dbSetting = new GlobalSetting { Key = key, Value = jsonValue };
                context.GlobalSettings.Add(dbSetting);
            }
            else
            {
                dbSetting.Value = jsonValue;
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // Rollback: Restore the previous value in memory
            lock (_lock)
            {
                if (previousSetting != null)
                {
                    _settings[key] = previousSetting;
                }
                else
                {
                    _settings.TryRemove(key, out _);
                }
            }
            throw; // Rethrow the exception
        }
    }

    public Task<ForumSettings?> GetForumSettingsAsync()
    {
        if (_settings.TryGetValue(GlobalSettingKeys.ForumSettings, out var setting) && !string.IsNullOrEmpty(setting.Value))
        {
            try
            {
                return Task.FromResult(JsonSerializer.Deserialize<ForumSettings>(setting.Value));
            }
            catch
            {
                return Task.FromResult<ForumSettings?>(null);
            }
        }

        return Task.FromResult<ForumSettings?>(null);
    }

    public async Task SetForumSettingsAsync(ForumSettings settings, ChatDbContext context, CancellationToken cancellationToken = default)
    {
        var jsonValue = JsonSerializer.Serialize(settings);
        var key = GlobalSettingKeys.ForumSettings;

        GlobalSetting? previousSetting = null;
        lock (_lock)
        {
            if (_settings.TryGetValue(key, out var existing))
            {
                previousSetting = new GlobalSetting
                {
                    Id = existing.Id,
                    Key = existing.Key,
                    Value = existing.Value
                };
            }

            var newSetting = new GlobalSetting
            {
                Id = previousSetting?.Id ?? 0,
                Key = key,
                Value = jsonValue
            };

            _settings[key] = newSetting;
        }

        try
        {
            var dbSetting = await context.GlobalSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
            if (dbSetting == null)
            {
                dbSetting = new GlobalSetting { Key = key, Value = jsonValue };
                context.GlobalSettings.Add(dbSetting);
            }
            else
            {
                dbSetting.Value = jsonValue;
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            lock (_lock)
            {
                if (previousSetting != null)
                {
                    _settings[key] = previousSetting;
                }
                else
                {
                    _settings.TryRemove(key, out _);
                }
            }
            throw;
        }
    }

    private static class GlobalSettingKeys
    {
        public const string LlmService = "LlmService";
        public const string ForumSettings = "ForumSettings";
    }
}