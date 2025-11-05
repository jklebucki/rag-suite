namespace RAG.Orchestrator.Api.Features.Chat.Prompting;

/// <summary>
/// Interface for building prompts for LLM interactions
/// </summary>
public interface IPromptBuilder
{
    /// <summary>
    /// Builds a contextual prompt for chat interactions
    /// </summary>
    /// <param name="context">Prompt context with user message, search results, and conversation history</param>
    /// <returns>Built prompt string</returns>
    string BuildContextualPrompt(PromptContext context);

    /// <summary>
    /// Builds a multilingual contextual prompt with strong language instructions
    /// </summary>
    /// <param name="context">Prompt context with user message, search results, and conversation history</param>
    /// <returns>Built multilingual prompt string</returns>
    string BuildMultilingualContextualPrompt(PromptContext context);

    /// <summary>
    /// Builds documents context for injection into user message
    /// </summary>
    /// <param name="searchResults">Search results to include</param>
    /// <param name="language">Response language</param>
    /// <returns>Documents context string</returns>
    string BuildDocumentsContext(RAG.Abstractions.Search.SearchResult[] searchResults, string language);
}

