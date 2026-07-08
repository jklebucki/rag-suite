namespace RAG.Orchestrator.Api.Features.Chat.Attachments;

public interface IContextTokenCounter
{
    int CountTokens(string text, string? model = null);
}
