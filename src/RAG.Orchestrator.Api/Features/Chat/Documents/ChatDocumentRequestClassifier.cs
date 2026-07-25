using RAG.DocumentProcessing.Abstractions;
using System.Globalization;
using System.Text;

namespace RAG.Orchestrator.Api.Features.Chat.Documents;

public static class ChatDocumentRequestClassifier
{
    private static readonly string[] TransformationPhrases =
    [
        "popraw",
        "skoryguj",
        "korekt",
        "edytuj",
        "przeredaguj",
        "zredaguj",
        "przetlumacz",
        "tlumacz",
        "podsumuj",
        "stresc",
        "zmien",
        "zastap",
        "usun",
        "dodaj",
        "correct",
        "proofread",
        "edit",
        "rewrite",
        "translate",
        "summarize",
        "summarise",
        "change",
        "replace",
        "remove",
        "add"
    ];

    private static readonly string[] CanonicalContentPhrases =
    [
        "pokaż zawartość",
        "pokaz zawartosc",
        "wyświetl zawartość",
        "wyswietl zawartosc",
        "pełna treść",
        "pelna tresc",
        "pełna zawartość",
        "pelna zawartosc",
        "cały dokument",
        "caly dokument",
        "tekst dokumentu",
        "wynik ocr",
        "ocr markdown",
        "show content",
        "show the content",
        "full text",
        "full document",
        "document text",
        "ocr result",
        "ocr markdown",
        "transcribe"
    ];

    public static bool RequestsCanonicalContent(string message)
    {
        var normalized = Normalize(message);
        return CanonicalContentPhrases.Any(phrase => normalized.Contains(phrase, StringComparison.Ordinal));
    }

    public static GeneratedArtifactFormat? GetRequestedExportFormat(string message)
    {
        var normalized = Normalize(message);
        if (normalized.Contains("docx", StringComparison.Ordinal) ||
            normalized.Contains("plik word", StringComparison.Ordinal) ||
            normalized.Contains("format word", StringComparison.Ordinal) ||
            normalized.Contains("dokument word", StringComparison.Ordinal) ||
            normalized.Contains("word document", StringComparison.Ordinal))
        {
            return GeneratedArtifactFormat.Docx;
        }

        return normalized.Contains(".txt", StringComparison.Ordinal) ||
               normalized.Contains("plik txt", StringComparison.Ordinal) ||
               normalized.Contains("plik tekstowy", StringComparison.Ordinal) ||
               normalized.Contains("text file", StringComparison.Ordinal)
            ? GeneratedArtifactFormat.Txt
            : null;
    }

    public static bool RequestsDirectCanonicalResponse(string message)
    {
        var normalized = Normalize(message);
        return !TransformationPhrases.Any(phrase => normalized.Contains(phrase, StringComparison.Ordinal)) &&
               (RequestsCanonicalContent(message) || GetRequestedExportFormat(message) != null);
    }

    private static string Normalize(string value)
    {
        var decomposition = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposition.Length);
        foreach (var character in decomposition)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character is 'ł' or 'Ł' ? 'l' : char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
