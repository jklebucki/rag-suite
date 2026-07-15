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
                    "Gdy użytkownik prosi o narysowanie, pokazanie lub wizualizację schematu procesu, zawsze umieść co najmniej jeden fenced block Mermaid z samym poprawnym kodem Mermaid. Linia otwierająca ma składać się dokładnie z trzech znaków backtick natychmiast połączonych ze słowem `mermaid`, bez spacji i wcięcia. Użyj `flowchart LR` albo `flowchart TD`; nie zastępuj diagramu zwykłym blokiem kodu ani opisem tekstowym.",
                    "Schemat Mermaid ma być jasny i bardzo techniczny: pokazuj odpowiednie komponenty lub usługi, kroki procesu, decyzje z opisanymi gałęziami, magazyny danych, interfejsy lub protokoły, kolejki, walidację oraz ścieżki błędów i ponowień, jeśli wynikają z kontekstu. Nie wymyślaj niepotwierdzonych elementów.",
                    "Koloruj węzły semantycznie za pomocą `classDef` i jawnych przypisań `class`: start/koniec — zielony, proces lub integracja — niebieski, decyzja — bursztynowy, dane — fioletowy, błąd — czerwony, system zewnętrzny — szary. Stosuj kontrastowe kolory HEX i spójną paletę w całym diagramie.",
                    "W bloku Mermaid nie używaj HTML ani Markdown, dyrektyw `click`, zewnętrznych odnośników ani dyrektyw inicjalizacyjnych. Używaj zwięzłych etykiet, jawnych kierunków strzałek i przed wysłaniem sprawdź poprawność składni Mermaid.",
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
                    "Ha a felhasználó folyamatábra rajzolását, megjelenítését vagy vizualizálását kéri, mindig adj meg legalább egy fenced Mermaid blokkot, amely csak érvényes Mermaid kódot tartalmaz. A nyitósor pontosan három backtick karakterből és az azokat szóköz vagy behúzás nélkül azonnal követő `mermaid` szóból álljon. Használj `flowchart LR` vagy `flowchart TD` típust; ne helyettesítsd általános kódblokkal vagy szöveges leírással.",
                    "A Mermaid diagram legyen világos és nagyon technikai: jelenítse meg a releváns komponenseket vagy szolgáltatásokat, folyamatlépéseket, címkézett döntési ágakat, adattárakat, interfészeket vagy protokollokat, sorokat, validációt, valamint a hiba- és újrapróbálkozási útvonalakat, ha azok a kontextusból következnek. Ne találj ki nem igazolt elemeket.",
                    "A csomópontokat szemantikusan színezd `classDef` és explicit `class` hozzárendelésekkel: kezdés/befejezés — zöld, folyamat vagy integráció — kék, döntés — borostyán, adat — lila, hiba — piros, külső rendszer — szürke. Használj kontrasztos HEX színeket és egységes palettát.",
                    "A Mermaid blokkon belül ne használj HTML-t vagy Markdown-t, `click` direktívát, külső hivatkozást vagy inicializálási direktívát. Használj tömör címkéket és egyértelmű nyílirányokat, majd küldés előtt ellenőrizd a Mermaid szintaxist.",
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
                    "Wanneer de gebruiker vraagt om een processchema te tekenen, tonen of visualiseren, voeg dan altijd minimaal één fenced Mermaid-block toe dat uitsluitend geldige Mermaid-code bevat. De openingsregel moet bestaan uit exact drie backticks, direct gevolgd door het woord `mermaid`, zonder spatie of inspringing. Gebruik `flowchart LR` of `flowchart TD`; vervang dit niet door een algemeen codeblok of alleen een tekstuele beschrijving.",
                    "Het Mermaid-diagram moet helder en zeer technisch zijn: toon relevante componenten of services, processtappen, beslissingen met benoemde vertakkingen, gegevensopslag, interfaces of protocollen, wachtrijen, validatie en fout- of retrypaden wanneer die uit de context volgen. Verzin geen onbevestigde onderdelen.",
                    "Kleur knooppunten semantisch met `classDef` en expliciete `class`-toewijzingen: start/einde — groen, proces of integratie — blauw, beslissing — amber, gegevens — paars, fout — rood, extern systeem — grijs. Gebruik contrasterende HEX-kleuren en één consistente palet.",
                    "Gebruik binnen het Mermaid-blok geen HTML of Markdown, `click`-instructies, externe links of initialisatiedirectieven. Gebruik korte labels en duidelijke pijlrichtingen en controleer vóór verzending de Mermaid-syntaxis.",
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
                    "Când utilizatorul cere desenarea, afișarea sau vizualizarea unei scheme de proces, include întotdeauna cel puțin un bloc fenced Mermaid care conține numai cod Mermaid valid. Linia de deschidere trebuie să conțină exact trei caractere backtick urmate imediat de cuvântul `mermaid`, fără spațiu sau indentare. Folosește `flowchart LR` sau `flowchart TD`; nu îl înlocui cu un bloc de cod generic sau doar cu o descriere textuală.",
                    "Diagrama Mermaid trebuie să fie clară și foarte tehnică: arată componentele sau serviciile relevante, pașii procesului, deciziile cu ramuri etichetate, depozitele de date, interfețele sau protocoalele, cozile, validarea și traseele de eroare sau reîncercare atunci când rezultă din context. Nu inventa elemente neconfirmate.",
                    "Colorează semantic nodurile folosind `classDef` și atribuiri explicite `class`: început/sfârșit — verde, proces sau integrare — albastru, decizie — chihlimbar, date — violet, eroare — roșu, sistem extern — gri. Folosește culori HEX contrastante și o paletă consecventă.",
                    "În blocul Mermaid nu folosi HTML sau Markdown, directive `click`, linkuri externe sau directive de inițializare. Folosește etichete concise și direcții clare ale săgeților și verifică sintaxa Mermaid înainte de trimitere.",
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
                    "When the user asks to draw, show, or visualize a process diagram, always include at least one fenced Mermaid block containing valid Mermaid code only. Its opening line must be exactly three backtick characters immediately followed by the word `mermaid`, with no spaces or indentation. Use `flowchart LR` or `flowchart TD`; do not replace it with a generic code block or a text-only description.",
                    "Make the Mermaid diagram clear and highly technical: show relevant components or services, process steps, decisions with labeled branches, data stores, interfaces or protocols, queues, validation, and error or retry paths when supported by the context. Do not invent unsupported elements.",
                    "Color nodes semantically with `classDef` and explicit `class` assignments: start/end — green, process or integration — blue, decision — amber, data — purple, error — red, external system — gray. Use high-contrast HEX colors and one consistent palette throughout the diagram.",
                    "Inside the Mermaid block, do not use HTML or Markdown, `click` directives, external links, or initialization directives. Use concise labels and explicit arrow directions, and validate Mermaid syntax before sending.",
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

