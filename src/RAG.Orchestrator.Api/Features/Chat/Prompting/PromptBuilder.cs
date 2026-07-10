using RAG.Abstractions.Search;
using RAG.Orchestrator.Api.Common.Constants;
using RAG.Orchestrator.Api.Common.Prompting;
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
        AppendServerDateTimeContext(promptBuilder);

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
        AppendMarkdownOutputContract(promptBuilder, context.ResponseLanguage);
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
        AppendServerDateTimeContext(promptBuilder);

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
        AppendMarkdownOutputContract(promptBuilder, context.ResponseLanguage);
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
        AppendServerDateTimeContext(contextBuilder);

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
        AppendMarkdownOutputContract(contextBuilder, language);
        contextBuilder.AppendLine(_languageService.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.Response,
            language));

        return contextBuilder.ToString();
    }

    private static void AppendMarkdownOutputContract(StringBuilder builder, string language)
    {
        var normalizedLanguage = (language ?? string.Empty).Trim().ToLowerInvariant();

        string title;
        string[] rules;

        switch (normalizedLanguage)
        {
            case "pl":
                title = "FORMAT ODPOWIEDZI (BEZWZGLĘDNY)";
                rules =
                [
                    "Zwracaj wyłącznie poprawny Markdown.",
                    "Nie zwracaj HTML, JSON ani XML.",
                    "Treść odpowiedzi musi zawierać co najmniej jeden jawny znacznik Markdown (np. nagłówek `##`, lista `-`, `**pogrubienie**` lub blok kodu).",
                    "OBOWIĄZKOWO: w OSTATNIEJ linii odpowiedzi dodaj temat rozmowy DOKŁADNIE w formacie: `CHAT_TITLE: krótki temat (3–6 słów)`, np. `CHAT_TITLE: tworzenie zamówienia zakupu w IFS`. Nie tłumacz słowa `CHAT_TITLE`, nie kopiuj przykładu dosłownie, nigdy nie pomijaj tej linii i nie dodawaj po niej żadnego tekstu."
                ];
                break;
            case "hu":
                title = "VÁLASZ FORMÁTUM (KÖTELEZŐ)";
                rules =
                [
                    "Csak érvényes Markdown formátumot használj.",
                    "Ne adj vissza HTML-, JSON- vagy XML-kimenetet.",
                    "A válasz tartalmazzon legalább egy egyértelmű Markdown elemet (pl. `##` címsor, `-` lista, `**félkövér**`, vagy kódblokk).",
                    "KÖTELEZŐ: a válasz UTOLSÓ soraként add meg a beszélgetés címét PONTOSAN ebben a formában: `CHAT_TITLE: rövid téma (3–6 szó)`, pl. `CHAT_TITLE: beszerzési rendelés létrehozása IFS-ben`. Ne fordítsd le a `CHAT_TITLE` szót, ne másold szó szerint a példát, soha ne hagyd ki ezt a sort, és ne írj utána semmit."
                ];
                break;
            case "nl":
                title = "ANTWOORDFORMAAT (VERPLICHT)";
                rules =
                [
                    "Geef uitsluitend geldige Markdown terug.",
                    "Geef geen HTML, JSON of XML terug.",
                    "Het antwoord moet minimaal één expliciet Markdown-element bevatten (bijv. `##` kop, `-` lijst, `**vet**` of codeblok).",
                    "VERPLICHT: voeg als LAATSTE regel van je antwoord de gesprekstitel toe, EXACT in dit formaat: `CHAT_TITLE: kort onderwerp (3–6 woorden)`, bijv. `CHAT_TITLE: inkooporder aanmaken in IFS`. Vertaal `CHAT_TITLE` niet, kopieer het voorbeeld niet letterlijk, laat deze regel nooit weg en zet er niets achter."
                ];
                break;
            case "ro":
                title = "FORMAT RĂSPUNS (OBLIGATORIU)";
                rules =
                [
                    "Răspunde exclusiv în Markdown valid.",
                    "Nu returna HTML, JSON sau XML.",
                    "Răspunsul trebuie să conțină cel puțin un element Markdown explicit (de ex. titlu `##`, listă `-`, `**bold**` sau bloc de cod).",
                    "OBLIGATORIU: pe ULTIMA linie a răspunsului adaugă titlul conversației EXACT în acest format: `CHAT_TITLE: subiect scurt (3–6 cuvinte)`, de ex. `CHAT_TITLE: creare comandă de achiziție în IFS`. Nu traduce `CHAT_TITLE`, nu copia exemplul literal, nu omite niciodată această linie și nu scrie nimic după ea."
                ];
                break;
            default:
                title = "RESPONSE FORMAT (MANDATORY)";
                rules =
                [
                    "Return valid Markdown only.",
                    "Do not return HTML, JSON, or XML.",
                    "The response body must include at least one explicit Markdown construct (for example: `##` heading, `-` list, `**bold**`, or a fenced code block).",
                    "MANDATORY: as the LAST line of your answer, add the conversation title EXACTLY in this format: `CHAT_TITLE: short topic (3–6 words)`, for example `CHAT_TITLE: creating a purchase order in IFS`. Do not translate `CHAT_TITLE`, do not copy the example literally, never omit this line, and write nothing after it."
                ];
                break;
        }

        builder.AppendLine($"=== {title} ===");
        foreach (var rule in rules)
        {
            builder.AppendLine($"- {rule}");
        }
    }

    private static void AppendServerDateTimeContext(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine(RuntimePromptContextBuilder.BuildServerDateTimeContext());
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

