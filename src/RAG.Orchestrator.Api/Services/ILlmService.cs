using RAG.Orchestrator.Api.Models;
namespace RAG.Orchestrator.Api.Services;

public interface ILlmService
{
    Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default);
    Task<(string response, int[]? context)> GenerateResponseWithContextAsync(string prompt, int[]? context = null, CancellationToken cancellationToken = default);

    // New methods for /api/chat endpoint with system message support
    Task<string> ChatAsync(string userMessage, string language = "en", CancellationToken cancellationToken = default);
    Task<string> ChatWithHistoryAsync(IEnumerable<LlmChatMessage> messageHistory, string userMessage, string language = "en", bool includeSystemMessage = false, CancellationToken cancellationToken = default);
    Task<string> GetSystemMessageAsync(string language = "en", CancellationToken cancellationToken = default);

    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    Task<string[]> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
}
