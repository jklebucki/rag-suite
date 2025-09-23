using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Models;

namespace RAG.Orchestrator.Api.Services;

/// <summary>
/// Interfejs dla singletona przechowującego ustawienia globalne w pamięci.
/// Zapewnia thread-safe dostęp do ustawień, szczególnie dla metod używanych w requestach chat.
/// Aktualizacja ustawień jest dwufazowa: najpierw w singletonie, potem w bazie danych.
/// </summary>
public interface IGlobalSettingsCache
{
    /// <summary>
    /// Inicjalizuje cache ładowaniem wszystkich ustawień z bazy danych.
    /// Wywoływane podczas startupu aplikacji.
    /// </summary>
    Task InitializeAsync(ChatDbContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera ustawienia LLM w sposób thread-safe.
    /// Używane przez requesty chat dla współbieżnego dostępu.
    /// </summary>
    Task<LlmSettings?> GetLlmSettingsAsync();

    /// <summary>
    /// Aktualizuje ustawienia LLM w dwufazowy sposób:
    /// 1. Najpierw aktualizuje w singletonie (w pamięci).
    /// 2. Po sukcesie zapisuje do bazy danych.
    /// W przypadku błędu bazy cofa zmiany w singletonie.
    /// </summary>
    Task SetLlmSettingsAsync(LlmSettings settings, ChatDbContext context, CancellationToken cancellationToken = default);
}