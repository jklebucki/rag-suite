using RAG.Abstractions.Search;

namespace RAG.Orchestrator.Api.Features.Chat.Prompting;

/// <summary>
/// Context for building prompts
/// </summary>
public record PromptContext
{
    /// <summary>
    /// User message content
    /// </summary>
    public required string UserMessage { get; init; }

    /// <summary>
    /// Search results from knowledge base
    /// </summary>
    public SearchResult[] SearchResults { get; init; } = Array.Empty<SearchResult>();

    /// <summary>
    /// Conversation history messages
    /// </summary>
    public IReadOnlyList<MessageContext> ConversationHistory { get; init; } = Array.Empty<MessageContext>();

    /// <summary>
    /// Response language code
    /// </summary>
    public required string ResponseLanguage { get; init; }

    /// <summary>
    /// Detected language of the user message
    /// </summary>
    public string? DetectedLanguage { get; init; }

    /// <summary>
    /// Whether document search is enabled
    /// </summary>
    public bool UseDocumentSearch { get; init; } = true;

    /// <summary>
    /// Whether documents are available
    /// </summary>
    public bool DocumentsAvailable { get; init; } = true;
}

/// <summary>
/// Message context for conversation history
/// </summary>
public record MessageContext
{
    /// <summary>
    /// Message role (user, assistant, system)
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// Message content
    /// </summary>
    public required string Content { get; init; }
}

