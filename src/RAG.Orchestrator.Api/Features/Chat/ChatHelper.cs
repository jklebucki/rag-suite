using RAG.Abstractions.Search;
using RAG.Orchestrator.Api.Localization;
using RAG.Orchestrator.Api.Models;
using System.Text;

namespace RAG.Orchestrator.Api.Features.Chat;

/// <summary>
/// Shared helper methods for chat services
/// </summary>
public static class ChatHelper
{
    /// <summary>
    /// Converts chat messages to LlmChatMessage format, excluding system messages
    /// </summary>
    public static List<LlmChatMessage> ConvertToLlmChatMessages(IEnumerable<ChatMessage> messages)
    {
        return messages
            .Where(m => m.Role == "user" || m.Role == "assistant")
            .Select(m => new LlmChatMessage
            {
                Role = m.Role,
                Content = m.Content
            })
            .ToList();
    }

    /// <summary>
    /// Converts user chat messages to LlmChatMessage format, excluding system messages
    /// </summary>
    public static List<LlmChatMessage> ConvertToLlmChatMessages(IEnumerable<UserChatMessage> messages)
    {
        return messages
            .Where(m => m.Role == "user" || m.Role == "assistant")
            .Select(m => new LlmChatMessage
            {
                Role = m.Role,
                Content = m.Content
            })
            .ToList();
    }

    /// <summary>
    /// Builds a contextual prompt for chat interactions
    /// </summary>
    public static string BuildContextualPrompt(
        string userMessage,
        SearchResult[] searchResults,
        List<ChatMessage> conversationHistory,
        ILanguageService languageService,
        string language = "en",
        bool useDocumentSearch = true)
    {
        var promptBuilder = new StringBuilder();

        // Add system instruction using localization based on document search setting
        var systemPrompt = useDocumentSearch
            ? languageService.GetLocalizedString("system_prompts", "rag_assistant", language)
            : languageService.GetLocalizedString("system_prompts", "rag_assistant_no_docs", language);

        var contextInstruction = useDocumentSearch
            ? languageService.GetLocalizedString("system_prompts", "context_instruction", language)
            : languageService.GetLocalizedString("system_prompts", "context_instruction_no_docs", language);

        promptBuilder.AppendLine(systemPrompt);
        promptBuilder.AppendLine(contextInstruction);

        // Add context from search results if available and enabled
        if (useDocumentSearch && searchResults.Length > 0)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(languageService.GetLocalizedString("system_prompts", "knowledge_base_context", language));

            // Add each document with its source information
            foreach (var result in searchResults)
            {
                var displayName = !string.IsNullOrEmpty(result.FileName) ? result.FileName : result.Source;
                promptBuilder.AppendLine($"[{displayName}]");
                promptBuilder.AppendLine(result.Content);
                promptBuilder.AppendLine();
            }

            // Add summary of sources used
            if (searchResults.Length > 1)
            {
                promptBuilder.AppendLine();
                promptBuilder.Append(FormatSourcesSummary(searchResults, languageService, language));
            }
        }
        else if (!useDocumentSearch)
        {
            promptBuilder.AppendLine();
            var noSearchNote = languageService.GetLocalizedString("system_prompts", "no_document_search_note", language)
                ?? "Note: Document search is disabled for this conversation.";
            promptBuilder.AppendLine($"=== UWAGA ===");
            promptBuilder.AppendLine(noSearchNote);
        }

        // Add recent conversation history (last 5 messages)
        var recentMessages = conversationHistory.TakeLast(5).ToArray();
        if (recentMessages.Length > 1) // More than just the current message
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(languageService.GetLocalizedString("system_prompts", "conversation_history", language));
            foreach (var msg in recentMessages.SkipLast(1))
            {
                var roleLabel = msg.Role == "user"
                    ? languageService.GetLocalizedString("ui_labels", "user", language)
                    : languageService.GetLocalizedString("ui_labels", "assistant", language);
                promptBuilder.AppendLine($"{roleLabel}: {msg.Content}");
            }
        }

        // Add current user message
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(languageService.GetLocalizedString("system_prompts", "current_question", language));
        var userLabel = languageService.GetLocalizedString("ui_labels", "user", language);
        promptBuilder.AppendLine($"{userLabel}: {userMessage}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(languageService.GetLocalizedString("system_prompts", "response", language));

        return promptBuilder.ToString();
    }

    /// <summary>
    /// Builds a contextual prompt for user chat interactions
    /// </summary>
    public static string BuildContextualPrompt(
        string userMessage,
        SearchResult[] searchResults,
        List<UserChatMessage> conversationHistory,
        ILanguageService languageService,
        string language = "en",
        bool useDocumentSearch = true)
    {
        var promptBuilder = new StringBuilder();

        // Add system instruction using localization based on document search setting
        var systemPrompt = useDocumentSearch
            ? languageService.GetLocalizedString("system_prompts", "rag_assistant", language)
            : languageService.GetLocalizedString("system_prompts", "rag_assistant_no_docs", language);

        var contextInstruction = useDocumentSearch
            ? languageService.GetLocalizedString("system_prompts", "context_instruction", language)
            : languageService.GetLocalizedString("system_prompts", "context_instruction_no_docs", language);

        promptBuilder.AppendLine(systemPrompt);
        promptBuilder.AppendLine(contextInstruction);

        // Add context from search results if available and enabled
        if (useDocumentSearch && searchResults.Length > 0)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(languageService.GetLocalizedString("system_prompts", "knowledge_base_context", language));

            // Add each document with its source information
            foreach (var result in searchResults)
            {
                var displayName = !string.IsNullOrEmpty(result.FileName) ? result.FileName : result.Source;
                promptBuilder.AppendLine($"[{displayName}]");
                promptBuilder.AppendLine(result.Content);
                promptBuilder.AppendLine();
            }

            // Add summary of sources used
            if (searchResults.Length > 1)
            {
                promptBuilder.AppendLine();
                promptBuilder.Append(FormatSourcesSummary(searchResults, languageService, language));
            }
        }
        else if (!useDocumentSearch)
        {
            promptBuilder.AppendLine();
            var noSearchNote = languageService.GetLocalizedString("system_prompts", "no_document_search_note", language)
                ?? "Note: Document search is disabled for this conversation.";
            promptBuilder.AppendLine($"=== UWAGA ===");
            promptBuilder.AppendLine(noSearchNote);
        }

        // Add recent conversation history (last 5 messages)
        var recentMessages = conversationHistory.TakeLast(5).ToArray();
        if (recentMessages.Length > 1) // More than just the current message
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(languageService.GetLocalizedString("system_prompts", "conversation_history", language));
            foreach (var msg in recentMessages.SkipLast(1))
            {
                var roleLabel = msg.Role == "user"
                    ? languageService.GetLocalizedString("ui_labels", "user", language)
                    : languageService.GetLocalizedString("ui_labels", "assistant", language);
                promptBuilder.AppendLine($"{roleLabel}: {msg.Content}");
            }
        }

        // Add current user message
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(languageService.GetLocalizedString("system_prompts", "current_question", language));
        var userLabel = languageService.GetLocalizedString("ui_labels", "user", language);
        promptBuilder.AppendLine($"{userLabel}: {userMessage}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(languageService.GetLocalizedString("system_prompts", "response", language));

        return promptBuilder.ToString();
    }

    /// <summary>
    /// Builds a multilingual contextual prompt
    /// </summary>
    public static string BuildMultilingualContextualPrompt(
        string userMessage,
        SearchResult[] searchResults,
        List<ChatMessage> conversationHistory,
        string detectedLanguage,
        string responseLanguage,
        ILanguageService languageService,
        bool useDocumentSearch = true,
        bool documentsAvailable = true)
    {
        var promptBuilder = new StringBuilder();

        // Get localized system prompt and instructions based on document search setting
        var systemPrompt = useDocumentSearch
            ? languageService.GetLocalizedSystemPrompt("rag_assistant", responseLanguage)
            : languageService.GetLocalizedString("system_prompts", "rag_assistant_no_docs", responseLanguage);

        var contextInstruction = useDocumentSearch
            ? languageService.GetLocalizedString("system_prompts", "context_instruction", responseLanguage)
            : languageService.GetLocalizedString("system_prompts", "context_instruction_no_docs", responseLanguage);

        promptBuilder.AppendLine(systemPrompt);
        promptBuilder.AppendLine(contextInstruction);
        promptBuilder.AppendLine();

        // Add context from search results with translation note if needed
        if (useDocumentSearch && documentsAvailable && searchResults.Length > 0)
        {
            var contextLabel = languageService.GetLocalizedString("system_prompts", "knowledge_base_context", responseLanguage);
            promptBuilder.AppendLine($"=== {contextLabel} ===");

            foreach (var result in searchResults)
            {
                var displayName = !string.IsNullOrEmpty(result.FileName) ? result.FileName : result.Source;
                promptBuilder.AppendLine($"[{displayName}]");
                promptBuilder.AppendLine(result.Content);
                promptBuilder.AppendLine();
            }
        }
        else if (!useDocumentSearch)
        {
            // Add note about document search being disabled
            var noSearchNote = languageService.GetLocalizedString("system_prompts", "no_document_search_note", responseLanguage)
                ?? "Note: Document search is disabled for this conversation.";
            promptBuilder.AppendLine($"=== UWAGA ===");
            promptBuilder.AppendLine(noSearchNote);
            promptBuilder.AppendLine();
        }
        else if (useDocumentSearch && !documentsAvailable)
        {
            // Add note about document database unavailability
            var unavailableNote = languageService.GetLocalizedString("system_prompts", "documents_unavailable", responseLanguage)
                ?? "Note: The document database is currently unavailable. Responses will be generated without reference documents.";
            promptBuilder.AppendLine($"=== UWAGA ===");
            promptBuilder.AppendLine(unavailableNote);
            promptBuilder.AppendLine();
        }

        // Add recent conversation history
        var recentMessages = conversationHistory.TakeLast(6).ToList();
        if (recentMessages.Count > 1)
        {
            var historyLabel = languageService.GetLocalizedString("system_prompts", "conversation_history", responseLanguage);
            promptBuilder.AppendLine($"=== {historyLabel} ===");

            var userLabel = languageService.GetLocalizedString("ui_labels", "user", responseLanguage);
            var assistantLabel = languageService.GetLocalizedString("ui_labels", "assistant", responseLanguage);

            foreach (var msg in recentMessages.TakeLast(4))
            {
                var roleLabel = msg.Role == "user" ? userLabel : assistantLabel;
                promptBuilder.AppendLine($"{roleLabel}: {msg.Content}");
            }
            promptBuilder.AppendLine();
        }

        // Add current question
        var questionLabel = languageService.GetLocalizedString("system_prompts", "current_question", responseLanguage);
        promptBuilder.AppendLine($"=== {questionLabel} ===");
        promptBuilder.AppendLine(userMessage);
        promptBuilder.AppendLine();

        // Add localized instructions based on document search setting
        var instructionsLabel = languageService.GetLocalizedString("ui_labels", "instructions", responseLanguage);
        promptBuilder.AppendLine($"=== {instructionsLabel} ===");

        var respondInLanguage = languageService.GetLocalizedString("instructions", "respond_in_language", responseLanguage);
        promptBuilder.AppendLine($"- {respondInLanguage}");

        if (useDocumentSearch)
        {
            var useKnowledgeBase = languageService.GetLocalizedString("instructions", "use_knowledge_base", responseLanguage);
            promptBuilder.AppendLine($"- {useKnowledgeBase}");
        }
        else
        {
            var useGeneralKnowledge = languageService.GetLocalizedString("instructions", "use_general_knowledge", responseLanguage);
            promptBuilder.AppendLine($"- {useGeneralKnowledge}");
        }

        var considerHistory = languageService.GetLocalizedString("instructions", "consider_history", responseLanguage);
        promptBuilder.AppendLine($"- {considerHistory}");

        var beHonest = useDocumentSearch
            ? languageService.GetLocalizedString("instructions", "be_honest", responseLanguage)
            : languageService.GetLocalizedString("instructions", "be_honest_no_docs", responseLanguage);
        promptBuilder.AppendLine($"- {beHonest}");

        var beHelpful = languageService.GetLocalizedString("instructions", "be_helpful", responseLanguage);
        promptBuilder.AppendLine($"- {beHelpful}");
        promptBuilder.AppendLine();

        // Add language-specific note if translation is involved
        if (detectedLanguage != responseLanguage)
        {
            var translationNote = languageService.GetLocalizedString("system_prompts", "translation_note", responseLanguage);
            promptBuilder.AppendLine($"[{translationNote}: {detectedLanguage} → {responseLanguage}]");
            promptBuilder.AppendLine();
        }

        var responseLabel = languageService.GetLocalizedString("system_prompts", "response", responseLanguage);
        promptBuilder.AppendLine($"{responseLabel}:");

        return promptBuilder.ToString();
    }

    /// <summary>
    /// Builds a multilingual contextual prompt for user chat
    /// </summary>
    public static string BuildMultilingualContextualPrompt(
        string userMessage,
        SearchResult[] searchResults,
        List<UserChatMessage> conversationHistory,
        string detectedLanguage,
        string responseLanguage,
        ILanguageService languageService,
        bool useDocumentSearch = true,
        bool documentsAvailable = true)
    {
        var promptBuilder = new StringBuilder();

        // Add recent conversation history
        var recentMessages = conversationHistory.TakeLast(6).ToArray();

        // CRITICAL: Strong language instruction at the beginning
        var languageInstruction = languageService.GetLocalizedString("instructions", "respond_in_language", responseLanguage);
        promptBuilder.AppendLine($"IMPORTANT: {languageInstruction}");
        promptBuilder.AppendLine($"MUST RESPOND IN: {responseLanguage.ToUpper()}");
        promptBuilder.AppendLine();

        // Add system instruction with language information using localization based on document search setting
        var systemPrompt = useDocumentSearch
            ? languageService.GetLocalizedString("system_prompts", "rag_assistant", responseLanguage)
            : languageService.GetLocalizedString("system_prompts", "rag_assistant_no_docs", responseLanguage);

        var contextInstruction = useDocumentSearch
            ? languageService.GetLocalizedString("system_prompts", "context_instruction", responseLanguage)
            : languageService.GetLocalizedString("system_prompts", "context_instruction_no_docs", responseLanguage);

        promptBuilder.AppendLine(systemPrompt);
        promptBuilder.AppendLine(contextInstruction);

        if (useDocumentSearch && documentsAvailable && searchResults.Length > 0)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(languageService.GetLocalizedString("system_prompts", "knowledge_base_context", responseLanguage));

            // Add each document with its source information
            foreach (var result in searchResults)
            {
                var displayName = !string.IsNullOrEmpty(result.FileName) ? result.FileName : result.Source;
                promptBuilder.AppendLine($"[{displayName}]");
                promptBuilder.AppendLine(result.Content);
                promptBuilder.AppendLine();
            }

            // Add summary of sources used
            if (searchResults.Length > 1)
            {
                promptBuilder.AppendLine();
                promptBuilder.Append(FormatSourcesSummary(searchResults, languageService, responseLanguage));
            }

            // Reinforce language instruction after context
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"REMINDER: {languageInstruction}");
        }
        else if (!useDocumentSearch)
        {
            promptBuilder.AppendLine();
            var noSearchNote = languageService.GetLocalizedString("system_prompts", "no_document_search_note", responseLanguage)
                ?? "Note: Document search is disabled for this conversation.";
            promptBuilder.AppendLine($"=== UWAGA ===");
            promptBuilder.AppendLine(noSearchNote);
            promptBuilder.AppendLine();
        }
        else
        {
            // Add note about document database unavailability
            var unavailableNote = languageService.GetLocalizedString("system_prompts", "documents_unavailable", responseLanguage)
                ?? "Note: The document database is currently unavailable. Responses will be generated without reference documents.";
            promptBuilder.AppendLine($"=== UWAGA ===");
            promptBuilder.AppendLine(unavailableNote);
            promptBuilder.AppendLine();
        }

        if (recentMessages.Length > 1)
        {
            var historyLabel = languageService.GetLocalizedString("system_prompts", "conversation_history", responseLanguage);
            promptBuilder.AppendLine($"=== {historyLabel} ===");

            var userLabel = languageService.GetLocalizedString("ui_labels", "user", responseLanguage);
            var assistantLabel = languageService.GetLocalizedString("ui_labels", "assistant", responseLanguage);

            foreach (var msg in recentMessages.TakeLast(4))
            {
                var roleLabel = msg.Role == "user" ? userLabel : assistantLabel;
                promptBuilder.AppendLine($"{roleLabel}: {msg.Content}");
            }
            promptBuilder.AppendLine();
        }

        // Add current question
        var questionLabel = languageService.GetLocalizedString("system_prompts", "current_question", responseLanguage);
        promptBuilder.AppendLine($"=== {questionLabel} ===");
        promptBuilder.AppendLine(userMessage);
        promptBuilder.AppendLine();

        // Add localized instructions based on document search setting
        var instructionsLabel = languageService.GetLocalizedString("ui_labels", "instructions", responseLanguage);
        promptBuilder.AppendLine($"=== {instructionsLabel} ===");

        var respondInLanguage = languageService.GetLocalizedString("instructions", "respond_in_language", responseLanguage);
        promptBuilder.AppendLine($"- {respondInLanguage}");

        if (useDocumentSearch)
        {
            var useKnowledgeBase = languageService.GetLocalizedString("instructions", "use_knowledge_base", responseLanguage);
            promptBuilder.AppendLine($"- {useKnowledgeBase}");
        }
        else
        {
            var useGeneralKnowledge = languageService.GetLocalizedString("instructions", "use_general_knowledge", responseLanguage);
            promptBuilder.AppendLine($"- {useGeneralKnowledge}");
        }

        var considerHistory = languageService.GetLocalizedString("instructions", "consider_history", responseLanguage);
        promptBuilder.AppendLine($"- {considerHistory}");

        var beHonest = useDocumentSearch
            ? languageService.GetLocalizedString("instructions", "be_honest", responseLanguage)
            : languageService.GetLocalizedString("instructions", "be_honest_no_docs", responseLanguage);
        promptBuilder.AppendLine($"- {beHonest}");

        var beHelpful = languageService.GetLocalizedString("instructions", "be_helpful", responseLanguage);
        promptBuilder.AppendLine($"- {beHelpful}");
        promptBuilder.AppendLine();

        // Add language-specific note if translation is involved
        if (detectedLanguage != responseLanguage)
        {
            var translationNote = languageService.GetLocalizedString("system_prompts", "translation_note", responseLanguage);
            promptBuilder.AppendLine($"[{translationNote}: {detectedLanguage} → {responseLanguage}]");
            promptBuilder.AppendLine();
        }

        var responseLabel = languageService.GetLocalizedString("system_prompts", "response", responseLanguage);
        promptBuilder.AppendLine($"{responseLabel}:");

        return promptBuilder.ToString();
    }

    /// <summary>
    /// Formats document source information for inclusion in prompts
    /// </summary>
    public static string FormatDocumentSource(SearchResult result, ILanguageService languageService, string language)
    {
        var displayName = !string.IsNullOrEmpty(result.FileName) ? result.FileName : result.Source;
        var sourceLabel = languageService.GetLocalizedString("ui_labels", "source", language) ?? "Source";

        if (result.Metadata != null && result.Metadata.TryGetValue("chunksFound", out var chunksFound) &&
            result.Metadata.TryGetValue("totalChunks", out var totalChunks))
        {
            return $"[{sourceLabel}: {displayName}, {chunksFound}/{totalChunks} chunks]";
        }

        return $"[{sourceLabel}: {displayName}]";
    }

    /// <summary>
    /// Creates a formatted list of all document sources used
    /// </summary>
    public static string FormatSourcesSummary(SearchResult[] searchResults, ILanguageService languageService, string language)
    {
        if (searchResults.Length == 0)
            return string.Empty;

        var sourcesLabel = languageService.GetLocalizedString("ui_labels", "sources", language) ?? "Sources";
        var usedLabel = languageService.GetLocalizedString("ui_labels", "used", language) ?? "used";

        var sources = searchResults
            .Select(r => !string.IsNullOrEmpty(r.FileName) ? r.FileName : r.Source)
            .Distinct()
            .ToArray();

        return $"{sourcesLabel} {usedLabel}: {string.Join(", ", sources)}";
    }

    /// <summary>
    /// Build documents context for injection into user message with multilingual support
    /// </summary>
    public static string BuildDocumentsContext(SearchResult[] searchResults, ILanguageService languageService, string language = "en")
    {
        if (searchResults.Length == 0)
            return string.Empty;

        var contextBuilder = new StringBuilder();
        var contextLabel = languageService.GetLocalizedString("system_prompts", "knowledge_base_context", language) ?? "Knowledge Base Context";

        contextBuilder.AppendLine($"=== {contextLabel} ===");

        foreach (var result in searchResults)
        {
            var displayName = !string.IsNullOrEmpty(result.FileName) ? result.FileName : result.Source;
            contextBuilder.AppendLine($"[{displayName}]");
            contextBuilder.AppendLine(result.Content);
            contextBuilder.AppendLine();
        }

        if (!string.IsNullOrEmpty(language) && language != "en")
        {
            var translationNote = languageService.GetLocalizedString("system_prompts", "translation_note", language);
            if (!string.IsNullOrEmpty(translationNote))
            {
                contextBuilder.AppendLine($"[{translationNote}]");
                contextBuilder.AppendLine();
            }
        }

        if (searchResults.Length > 1)
        {
            var beHonestInstruction = languageService.GetLocalizedString("instructions", "be_honest", language);
            if (!string.IsNullOrEmpty(beHonestInstruction))
            {
                contextBuilder.AppendLine($"[{beHonestInstruction}]");
            }
        }

        return contextBuilder.ToString();
    }

    /// <summary>
    /// Gets response metadata dictionary
    /// </summary>
    public static Dictionary<string, object> GetResponseMetadata(
        int searchResultsCount,
        bool enabledTranslation,
        bool documentsAvailable,
        string responseLanguage,
        bool useDocumentSearch = true)
    {
        var metadata = new Dictionary<string, object>
        {
            ["searchResultsCount"] = searchResultsCount,
            ["enabledTranslation"] = enabledTranslation,
            ["documentsAvailable"] = documentsAvailable,
            ["useDocumentSearch"] = useDocumentSearch
        };

        if (!useDocumentSearch)
        {
            metadata["note"] = "Document search was disabled for this conversation";
        }
        else if (!documentsAvailable)
        {
            metadata["note"] = "Document database was unavailable during response generation";
        }

        return metadata;
    }
}
