using RAG.Abstractions.Search;
using RAG.Orchestrator.Api.Common.Constants;
using RAG.Orchestrator.Api.Localization;
using System.Text;

namespace RAG.Orchestrator.Api.Features.Chat.Prompting;

/// <summary>
/// Builder for creating prompts for LLM interactions
/// </summary>
public class PromptBuilder : IPromptBuilder
{
    private readonly ILanguageService _languageService;

    public PromptBuilder(ILanguageService languageService)
    {
        _languageService = languageService;
    }

    /// <inheritdoc />
    public string BuildContextualPrompt(PromptContext context)
    {
        var promptBuilder = new StringBuilder();

        // Add system instruction using localization based on document search setting
        var systemPrompt = context.UseDocumentSearch
            ? _languageService.GetLocalizedString(
                "system_prompts",
                LocalizationKeys.SystemPrompts.RagAssistant,
                context.ResponseLanguage)
            : _languageService.GetLocalizedString(
                "system_prompts",
                LocalizationKeys.SystemPrompts.RagAssistantNoDocs,
                context.ResponseLanguage);

        var contextInstruction = context.UseDocumentSearch
            ? _languageService.GetLocalizedString(
                "system_prompts",
                LocalizationKeys.SystemPrompts.ContextInstruction,
                context.ResponseLanguage)
            : _languageService.GetLocalizedString(
                "system_prompts",
                LocalizationKeys.SystemPrompts.ContextInstructionNoDocs,
                context.ResponseLanguage);

        promptBuilder.AppendLine(systemPrompt);
        promptBuilder.AppendLine(contextInstruction);

        // Add context from search results if available and enabled
        if (context.UseDocumentSearch && context.SearchResults.Length > 0)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(_languageService.GetLocalizedString(
                LocalizationKeys.SystemPrompts.KnowledgeBaseContext,
                LocalizationKeys.SystemPrompts.KnowledgeBaseContext,
                context.ResponseLanguage));

            // Add each document with its source information
            foreach (var result in context.SearchResults)
            {
                var displayName = !string.IsNullOrEmpty(result.FileName) ? result.FileName : result.Source;
                promptBuilder.AppendLine($"[{displayName}]");
                promptBuilder.AppendLine(result.Content);
                promptBuilder.AppendLine();
            }

            // Add summary of sources used
            if (context.SearchResults.Length > 1)
            {
                promptBuilder.AppendLine();
                promptBuilder.Append(FormatSourcesSummary(context.SearchResults, context.ResponseLanguage));
            }
        }
        else if (!context.UseDocumentSearch)
        {
            promptBuilder.AppendLine();
            var noSearchNote = _languageService.GetLocalizedString(
                LocalizationKeys.SystemPrompts.NoDocumentSearchNote,
                LocalizationKeys.SystemPrompts.NoDocumentSearchNote,
                context.ResponseLanguage)
                ?? "Note: Document search is disabled for this conversation.";
            promptBuilder.AppendLine($"=== {noSearchNote} ===");
            promptBuilder.AppendLine(noSearchNote);
        }

        // Add recent conversation history (last 5 messages)
        var recentMessages = context.ConversationHistory.TakeLast(5).ToArray();
        if (recentMessages.Length > 1)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(_languageService.GetLocalizedString(
                "system_prompts",
                LocalizationKeys.SystemPrompts.ConversationHistory,
                context.ResponseLanguage));

            foreach (var msg in recentMessages.SkipLast(1))
            {
                var roleLabel = msg.Role == ChatRoles.User
                    ? _languageService.GetLocalizedString(
                        "ui_labels",
                        LocalizationKeys.UiLabels.User,
                        context.ResponseLanguage)
                    : _languageService.GetLocalizedString(
                        "ui_labels",
                        LocalizationKeys.UiLabels.Assistant,
                        context.ResponseLanguage);
                promptBuilder.AppendLine($"{roleLabel}: {msg.Content}");
            }
        }

        // Add current user message
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(_languageService.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.CurrentQuestion,
            context.ResponseLanguage));
        var userLabel = _languageService.GetLocalizedString(
            "ui_labels",
            LocalizationKeys.UiLabels.User,
            context.ResponseLanguage);
        promptBuilder.AppendLine($"{userLabel}: {context.UserMessage}");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine(_languageService.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.Response,
            context.ResponseLanguage));

        return promptBuilder.ToString();
    }

    /// <inheritdoc />
    public string BuildMultilingualContextualPrompt(PromptContext context)
    {
        var promptBuilder = new StringBuilder();

        // CRITICAL: Strong language instruction at the beginning
        var languageInstruction = _languageService.GetLocalizedString(
            "instructions",
            LocalizationKeys.Instructions.RespondInLanguage,
            context.ResponseLanguage);
        promptBuilder.AppendLine($"IMPORTANT: {languageInstruction}");
        promptBuilder.AppendLine($"MUST RESPOND IN: {context.ResponseLanguage.ToUpper()}");
        promptBuilder.AppendLine();

        // Add system instruction with language information using localization based on document search setting
        var systemPrompt = context.UseDocumentSearch
            ? _languageService.GetLocalizedString(
                "system_prompts",
                LocalizationKeys.SystemPrompts.RagAssistant,
                context.ResponseLanguage)
            : _languageService.GetLocalizedString(
                "system_prompts",
                LocalizationKeys.SystemPrompts.RagAssistantNoDocs,
                context.ResponseLanguage);

        var contextInstruction = context.UseDocumentSearch
            ? _languageService.GetLocalizedString(
                "system_prompts",
                LocalizationKeys.SystemPrompts.ContextInstruction,
                context.ResponseLanguage)
            : _languageService.GetLocalizedString(
                "system_prompts",
                LocalizationKeys.SystemPrompts.ContextInstructionNoDocs,
                context.ResponseLanguage);

        promptBuilder.AppendLine(systemPrompt);
        promptBuilder.AppendLine(contextInstruction);

        if (context.UseDocumentSearch && context.DocumentsAvailable && context.SearchResults.Length > 0)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(_languageService.GetLocalizedString(
                "system_prompts",
                LocalizationKeys.SystemPrompts.KnowledgeBaseContext,
                context.ResponseLanguage));

            // Add each document with its source information
            foreach (var result in context.SearchResults)
            {
                var displayName = !string.IsNullOrEmpty(result.FileName) ? result.FileName : result.Source;
                promptBuilder.AppendLine($"[{displayName}]");
                promptBuilder.AppendLine(result.Content);
                promptBuilder.AppendLine();
            }

            // Add summary of sources used
            if (context.SearchResults.Length > 1)
            {
                promptBuilder.AppendLine();
                promptBuilder.Append(FormatSourcesSummary(context.SearchResults, context.ResponseLanguage));
            }

            // Reinforce language instruction after context
            promptBuilder.AppendLine();
            promptBuilder.AppendLine($"REMINDER: {languageInstruction}");
        }
        else if (!context.UseDocumentSearch)
        {
            promptBuilder.AppendLine();
            var noSearchNote = _languageService.GetLocalizedString(
                "system_prompts",
                LocalizationKeys.SystemPrompts.NoDocumentSearchNote,
                context.ResponseLanguage)
                ?? "Note: Document search is disabled for this conversation.";
            promptBuilder.AppendLine($"=== {noSearchNote} ===");
            promptBuilder.AppendLine(noSearchNote);
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(_languageService.GetLocalizedString(
                "instructions",
                LocalizationKeys.Instructions.BeHonestNoDocs,
                context.ResponseLanguage));
        }
        else
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(_languageService.GetLocalizedString(
                "instructions",
                LocalizationKeys.Instructions.BeHonest,
                context.ResponseLanguage));
        }

        // Add recent conversation history
        var recentMessages = context.ConversationHistory.TakeLast(5).ToArray();
        if (recentMessages.Length > 1)
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine(_languageService.GetLocalizedString(
                "system_prompts",
                LocalizationKeys.SystemPrompts.ConversationHistory,
                context.ResponseLanguage));
            foreach (var msg in recentMessages.SkipLast(1))
            {
                var roleLabel = msg.Role == ChatRoles.User
                    ? _languageService.GetLocalizedString(
                        "ui_labels",
                        LocalizationKeys.UiLabels.User,
                        context.ResponseLanguage)
                    : _languageService.GetLocalizedString(
                        "ui_labels",
                        LocalizationKeys.UiLabels.Assistant,
                        context.ResponseLanguage);
                promptBuilder.AppendLine($"{roleLabel}: {msg.Content}");
            }
        }

        promptBuilder.AppendLine();
        promptBuilder.AppendLine(_languageService.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.CurrentQuestion,
            context.ResponseLanguage));
        var userLabel = _languageService.GetLocalizedString(
            "ui_labels",
            LocalizationKeys.UiLabels.User,
            context.ResponseLanguage);
        promptBuilder.AppendLine($"{userLabel} ({context.DetectedLanguage ?? context.ResponseLanguage}): {context.UserMessage}");
        promptBuilder.AppendLine();

        // FINAL CRITICAL REMINDER before response
        promptBuilder.AppendLine($"CRITICAL: {languageInstruction}");
        promptBuilder.AppendLine(_languageService.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.Response,
            context.ResponseLanguage));

        return promptBuilder.ToString();
    }

    /// <inheritdoc />
    public string BuildDocumentsContext(SearchResult[] searchResults, string language)
    {
        if (searchResults.Length == 0)
            return string.Empty;

        var contextBuilder = new StringBuilder();
        var prePrompt = _languageService.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.RagAssistant,
            language) ?? "";
        
        if (!string.IsNullOrEmpty(prePrompt))
        {
            contextBuilder.AppendLine(prePrompt);
            contextBuilder.AppendLine();
        }

        var languageInstruction = _languageService.GetLocalizedString(
            "instructions",
            LocalizationKeys.Instructions.RespondInLanguage,
            language);
        contextBuilder.AppendLine($"IMPORTANT: {languageInstruction}");
        contextBuilder.AppendLine($"MUST RESPOND IN: {language.ToUpper()}");

        // Add context header using localization
        var contextHeader = _languageService.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.KnowledgeBaseContext,
            language) ?? "=== KNOWLEDGE BASE CONTEXT ===";
        contextBuilder.AppendLine(contextHeader);
        contextBuilder.AppendLine();

        foreach (var result in searchResults)
        {
            // Format document source
            var displayName = !string.IsNullOrEmpty(result.FileName) ? result.FileName : result.Source;
            var sourceLabel = _languageService.GetLocalizedString(
                "ui_labels",
                LocalizationKeys.UiLabels.Source,
                language) ?? "Source";

            contextBuilder.AppendLine($"[{sourceLabel}: {displayName}]");
            contextBuilder.AppendLine($"- {result.Content}");
            contextBuilder.AppendLine();
        }

        // Add summary of sources used if multiple
        if (searchResults.Length > 1)
        {
            contextBuilder.Append(FormatSourcesSummary(searchResults, language));
            contextBuilder.AppendLine();

            // Add honesty instruction
            var beHonestInstruction = _languageService.GetLocalizedString(
                "instructions",
                LocalizationKeys.Instructions.BeHonest,
                language);
            if (!string.IsNullOrEmpty(beHonestInstruction))
            {
                contextBuilder.AppendLine($"REMINDER: {beHonestInstruction}");
            }
        }

        // Add context footer using localization
        var contextFooter = _languageService.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.DocumentSourceIntro,
            language) ?? "=== END OF KNOWLEDGE BASE CONTEXT ===";
        contextBuilder.AppendLine(contextFooter);
        contextBuilder.AppendLine($"CRITICAL: {languageInstruction}");
        contextBuilder.AppendLine(_languageService.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.Response,
            language));

        return contextBuilder.ToString();
    }

    private string FormatSourcesSummary(SearchResult[] searchResults, string language)
    {
        if (searchResults.Length == 0)
            return string.Empty;

        var sourcesLabel = _languageService.GetLocalizedString(
            "ui_labels",
            LocalizationKeys.UiLabels.Sources,
            language) ?? "Sources";
        var usedLabel = _languageService.GetLocalizedString(
            "ui_labels",
            LocalizationKeys.UiLabels.Used,
            language) ?? "used";

        var sources = searchResults
            .Select(r => !string.IsNullOrEmpty(r.FileName) ? r.FileName : r.Source)
            .Distinct()
            .ToArray();

        return $"{sourcesLabel} {usedLabel}: {string.Join(", ", sources)}";
    }
}

