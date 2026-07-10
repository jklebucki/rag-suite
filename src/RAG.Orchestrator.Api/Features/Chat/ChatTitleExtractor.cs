using System.Text.RegularExpressions;

namespace RAG.Orchestrator.Api.Features.Chat;

/// <summary>
/// Extracts a conversation title that the LLM appends to its own answer (no separate LLM call),
/// and provides a deterministic fallback derived from the user's message.
///
/// Design notes (why this is tolerant rather than strict):
/// - The title is marked with an unambiguous, language-agnostic sentinel (<c>CHAT_TITLE:</c>) on its own
///   line. This avoids the collisions that a bare <c>{...}</c> delimiter causes in a technical assistant
///   whose answers routinely contain code, JSON and braces.
/// - Parsing is lenient: any short phrase is accepted (not an exact word count), because small local
///   models rarely hit a rigid "exactly N words" rule — that was the main reason titles were dropped.
/// - A legacy <c>{...}</c> final line is still accepted as a secondary marker for backward compatibility.
/// </summary>
public static class ChatTitleExtractor
{
    public const string TitleMarker = "CHAT_TITLE";

    private const int MaxTitleChars = 80;
    private const int MaxTitleWords = 12;
    private const int MaxFallbackChars = 60;

    // Matches a final marker line like "CHAT_TITLE: tworzenie zamówienia zakupu" (case-insensitive).
    private static readonly Regex MarkerLineRegex = new(
        @"^\s*" + TitleMarker + @"\s*[:\-–—]\s*(?<title>.+?)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Legacy secondary marker: a line that is entirely "{ ... }".
    private static readonly Regex LegacyBraceLineRegex = new(
        @"^\s*\{(?<title>[^{}]+)\}\s*$",
        RegexOptions.Compiled);

    /// <summary>
    /// Extracts the title from the response and returns the response with the marker line removed.
    /// Title is null when no valid marker is present.
    /// </summary>
    public static (string CleanedResponse, string? Title) Extract(string? response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return (response ?? string.Empty, null);
        }

        var normalized = response.Replace("\r\n", "\n").Replace("\r", "\n");
        var lines = normalized.Split('\n');

        // Primary: the last line carrying the explicit CHAT_TITLE marker, anywhere in the response
        // (tolerant to a stray trailing line the model might append after it).
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var marker = MarkerLineRegex.Match(lines[i]);
            if (marker.Success)
            {
                return BuildResult(lines, i, marker.Groups["title"].Value);
            }
        }

        // Secondary (legacy): a lone "{ ... }" as the last non-empty line only, to limit collisions
        // with braces that appear inside code/JSON in the body.
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            var brace = LegacyBraceLineRegex.Match(lines[i]);
            return brace.Success
                ? BuildResult(lines, i, brace.Groups["title"].Value)
                : (response, null);
        }

        return (response, null);
    }

    /// <summary>
    /// Deterministic fallback title built from the user's message (first line, trimmed and truncated).
    /// Guarantees a meaningful title even when the model omits the marker.
    /// </summary>
    public static string BuildFallbackTitle(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var firstLine = message.Replace("\r", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        firstLine = Regex.Replace(firstLine, @"\s+", " ").Trim();

        if (firstLine.Length > MaxFallbackChars)
        {
            firstLine = firstLine[..MaxFallbackChars].TrimEnd() + "…";
        }

        return firstLine;
    }

    private static (string CleanedResponse, string? Title) BuildResult(string[] lines, int index, string rawTitle)
    {
        var cleaned = RemoveLine(lines, index);
        var title = Sanitize(rawTitle);
        if (string.IsNullOrWhiteSpace(title) || IsPlaceholder(title))
        {
            return (cleaned, null);
        }

        return (cleaned, title);
    }

    private static string RemoveLine(string[] lines, int index)
    {
        var kept = new List<string>(lines.Length);
        for (var i = 0; i < lines.Length; i++)
        {
            if (i != index)
            {
                kept.Add(lines[i]);
            }
        }

        return string.Join("\n", kept).TrimEnd();
    }

    private static string Sanitize(string value)
    {
        var title = value.Trim();

        // Strip wrapping quotes/brackets/emphasis the model sometimes adds.
        title = title.Trim('"', '\'', '`', '*', '{', '}', '[', ']', '(', ')', ' ', '.', '#');
        title = Regex.Replace(title, @"\s+", " ").Trim();

        if (title.Length == 0)
        {
            return title;
        }

        var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > MaxTitleWords)
        {
            title = string.Join(' ', words[..MaxTitleWords]);
        }

        if (title.Length > MaxTitleChars)
        {
            title = title[..MaxTitleChars].TrimEnd() + "…";
        }

        return title;
    }

    private static bool IsPlaceholder(string value)
    {
        var normalized = Regex.Replace(value.ToLowerInvariant(), @"[^\p{L}\p{N}\s]", " ");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized is
            "chat title" or
            "temat rozmowy" or
            "temat czatu" or
            "krotki temat rozmowy" or
            "krótki temat rozmowy" or
            "3 6 slow opisujacych temat" or
            "topic of this conversation" or
            "short conversation topic" or
            "pięć słów podsumowania" or
            "piec slow podsumowania" or
            "five word summary" or
            "summary in five words";
    }
}
