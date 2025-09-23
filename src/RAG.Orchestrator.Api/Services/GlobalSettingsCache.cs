using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Models;

namespace RAG.Orchestrator.Api.Services;

/// <summary>
/// Singleton przechowujący ustawienia globalne w pamięci.
/// Ładuje dane z bazy przy uruchomieniu i zapewnia thread-safe dostęp.
/// Aktualizacja jest dwufazowa: najpierw w pamięci, potem w bazie z obsługą rollback.
/// </summary>
public class GlobalSettingsCache : IGlobalSettingsCache
{
    private readonly ConcurrentDictionary<string, GlobalSetting> _settings = new();
    private readonly object _lock = new(); // Dla złożonych operacji wymagających synchronizacji

    /// <summary>
    /// Inicjalizuje cache ładowaniem wszystkich ustawień z bazy danych.
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
    /// Pobiera ustawienia LLM w sposób thread-safe.
    /// </summary>
    public Task<LlmSettings?> GetLlmSettingsAsync()
    {
        if (_settings.TryGetValue("LlmService", out var setting) && !string.IsNullOrEmpty(setting.Value))
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
    /// Aktualizuje ustawienia LLM w dwufazowy sposób.
    /// </summary>
    public async Task SetLlmSettingsAsync(LlmSettings settings, ChatDbContext context, CancellationToken cancellationToken = default)
    {
        var jsonValue = JsonSerializer.Serialize(settings);
        var key = "LlmService";

        // Faza 1: Aktualizacja w pamięci (z pamięcią poprzedniej wartości dla rollback)
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
                Id = previousSetting?.Id ?? 0, // Id zostanie ustawione przez EF przy zapisie
                Key = key,
                Value = jsonValue
            };
            _settings[key] = newSetting;
        }

        // Faza 2: Zapis do bazy
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
            // Rollback: Przywróć poprzednią wartość w pamięci
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
            throw; // Przekaż wyjątek dalej
        }
    }
}