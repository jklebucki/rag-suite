using System.Text.RegularExpressions;

namespace RAG.DocumentProcessing.Providers.Docling;

internal static partial class OcrTextQualityEvaluator
{
    public static double Calculate(string markdown, string plainText, int pageCount)
    {
        var text = plainText.Length >= markdown.Length ? plainText : markdown;
        var trimmedText = text.Trim();
        var pages = Math.Max(1, pageCount);
        var densityScore = Math.Clamp(trimmedText.Length / (pages * 500d), 0d, 1d);
        var words = WordPattern().Matches(trimmedText);
        if (words.Count == 0)
        {
            return densityScore * 0.35d;
        }

        var singleCharacterWords = 0;
        var shortWords = 0;
        foreach (Match word in words)
        {
            var characterCount = word.Value.EnumerateRunes().Count();
            if (characterCount == 1)
            {
                singleCharacterWords++;
            }

            if (characterCount <= 2)
            {
                shortWords++;
            }
        }

        var singleCharacterRatio = singleCharacterWords / (double)words.Count;
        var shortWordRatio = shortWords / (double)words.Count;
        var singleCharacterScore = 1d - Math.Clamp(singleCharacterRatio / 0.25d, 0d, 1d);
        var shortWordScore = 1d - Math.Clamp((shortWordRatio - 0.1d) / 0.35d, 0d, 1d);
        var lexicalScore = (singleCharacterScore * 0.6d) + (shortWordScore * 0.4d);
        var replacementCharacterRatio = trimmedText.Length == 0
            ? 1d
            : trimmedText.Count(character => character == '\uFFFD') / (double)trimmedText.Length;
        var characterIntegrityScore = 1d - Math.Clamp(replacementCharacterRatio / 0.01d, 0d, 1d);

        return Math.Clamp(
            (densityScore * 0.35d) + (lexicalScore * 0.55d) + (characterIntegrityScore * 0.1d),
            0d,
            1d);
    }

    [GeneratedRegex(@"\p{L}+", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();
}
