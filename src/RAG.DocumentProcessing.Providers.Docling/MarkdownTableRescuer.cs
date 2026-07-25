using System.Text.RegularExpressions;

namespace RAG.DocumentProcessing.Providers.Docling;

internal static partial class MarkdownTableRescuer
{
    public static bool NeedsFallback(string markdown, double minimumCompleteness)
    {
        return ExtractTables(markdown).Any(table => table.Score < minimumCompleteness);
    }

    public static string MergeBetterTables(
        string primaryMarkdown,
        string fallbackMarkdown,
        double minimumCompleteness,
        out int replacementCount)
    {
        var normalizedPrimary = primaryMarkdown.ReplaceLineEndings("\n");
        var normalizedFallback = fallbackMarkdown.ReplaceLineEndings("\n");
        var primaryLines = normalizedPrimary.Split('\n').ToList();
        var primaryTables = ExtractTables(normalizedPrimary);
        var fallbackTables = ExtractTables(normalizedFallback);
        replacementCount = 0;

        var comparableTableCount = Math.Min(primaryTables.Count, fallbackTables.Count);
        for (var tableIndex = comparableTableCount - 1; tableIndex >= 0; tableIndex--)
        {
            var primary = primaryTables[tableIndex];
            var fallback = fallbackTables[tableIndex];
            if (primary.Score >= minimumCompleteness ||
                fallback.Score <= primary.Score + 0.1d ||
                fallback.RowCount != primary.RowCount)
            {
                continue;
            }

            primaryLines.RemoveRange(primary.StartLine, primary.EndLine - primary.StartLine + 1);
            primaryLines.InsertRange(primary.StartLine, fallback.Lines);
            replacementCount++;
        }

        return replacementCount == 0 ? primaryMarkdown : string.Join('\n', primaryLines);
    }

    private static IReadOnlyList<(int StartLine, int EndLine, string[] Lines, int RowCount, double Score)> ExtractTables(
        string markdown)
    {
        var lines = markdown.ReplaceLineEndings("\n").Split('\n');
        var tables = new List<(int StartLine, int EndLine, string[] Lines, int RowCount, double Score)>();
        for (var lineIndex = 0; lineIndex < lines.Length - 1; lineIndex++)
        {
            if (!IsTableRow(lines[lineIndex]) || !IsSeparatorRow(lines[lineIndex + 1]))
            {
                continue;
            }

            var endLine = lineIndex + 1;
            while (endLine + 1 < lines.Length && IsTableRow(lines[endLine + 1]))
            {
                endLine++;
            }

            var tableLines = lines[lineIndex..(endLine + 1)];
            var rowCount = tableLines.Length - 1;
            tables.Add((lineIndex, endLine, tableLines, rowCount, CalculateScore(tableLines)));
            lineIndex = endLine;
        }

        return tables;
    }

    private static double CalculateScore(IReadOnlyList<string> lines)
    {
        var expectedColumnCount = GetCells(lines[1]).Count;
        if (expectedColumnCount < 2 || lines.Count < 3)
        {
            return 0d;
        }

        var contentRows = lines.Where((_, index) => index != 1)
            .Select(GetCells)
            .ToArray();
        var dataRows = contentRows.Skip(1).ToArray();
        var totalCells = expectedColumnCount * dataRows.Length;
        var populatedCells = dataRows.Sum(row => row.Take(expectedColumnCount).Count(cell => !string.IsNullOrWhiteSpace(cell)));
        var completeness = totalCells == 0 ? 0d : populatedCells / (double)totalCells;
        var consistentRows = contentRows.Count(row => row.Count == expectedColumnCount);
        var consistency = consistentRows / (double)contentRows.Length;
        var populatedHeaderCells = contentRows[0].Take(expectedColumnCount).Count(cell => !string.IsNullOrWhiteSpace(cell));
        var headerCompleteness = populatedHeaderCells / (double)expectedColumnCount;

        return (completeness * 0.65d) + (consistency * 0.2d) + (headerCompleteness * 0.15d);
    }

    private static bool IsTableRow(string line)
    {
        return line.Count(character => character == '|') >= 2;
    }

    private static bool IsSeparatorRow(string line)
    {
        var cells = GetCells(line);
        return cells.Count >= 2 && cells.All(cell => SeparatorCellPattern().IsMatch(cell));
    }

    private static IReadOnlyList<string> GetCells(string line)
    {
        var cells = line.Split('|').Select(cell => cell.Trim()).ToList();
        if (cells.Count > 0 && cells[0].Length == 0)
        {
            cells.RemoveAt(0);
        }

        if (cells.Count > 0 && cells[^1].Length == 0)
        {
            cells.RemoveAt(cells.Count - 1);
        }

        return cells;
    }

    [GeneratedRegex(@"^:?-{3,}:?$", RegexOptions.CultureInvariant)]
    private static partial Regex SeparatorCellPattern();
}
