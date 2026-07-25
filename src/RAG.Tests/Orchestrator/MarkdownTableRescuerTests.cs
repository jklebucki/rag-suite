using RAG.DocumentProcessing.Providers.Docling;

namespace RAG.Tests.Orchestrator;

public class MarkdownTableRescuerTests
{
    [Fact]
    public void MergeBetterTables_ReplacesOnlyIncompleteTable()
    {
        const string primary = """
            Poprawnie rozpoznany polski akapit.

            | | | ee |
            | --- | --- | --- |
            | Handel | HMF-100790 | |
            | Finanse | FKF-101358 | |

            Poprawne zakończenie.
            """;
        const string fallback = """
            uszkodzony tekst zapasowy

            | Produkt | Licencja | Liczba |
            | --- | --- | --- |
            | Handel | HMF-100790 | 6 |
            | Finanse | FKF-101358 | 19 |
            """;

        var merged = MarkdownTableRescuer.MergeBetterTables(
            primary,
            fallback,
            minimumCompleteness: 0.85d,
            out var replacementCount);

        Assert.Equal(1, replacementCount);
        Assert.Contains("Poprawnie rozpoznany polski akapit.", merged, StringComparison.Ordinal);
        Assert.Contains("| Handel | HMF-100790 | 6 |", merged, StringComparison.Ordinal);
        Assert.Contains("Poprawne zakończenie.", merged, StringComparison.Ordinal);
        Assert.DoesNotContain("uszkodzony tekst zapasowy", merged, StringComparison.Ordinal);
    }

    [Fact]
    public void NeedsFallback_WhenTableIsComplete_ReturnsFalse()
    {
        const string markdown = """
            | Produkt | Licencja | Liczba |
            | --- | --- | --- |
            | Handel | HMF-100790 | 6 |
            | Finanse | FKF-101358 | 19 |
            """;

        var needsFallback = MarkdownTableRescuer.NeedsFallback(markdown, minimumCompleteness: 0.85d);

        Assert.False(needsFallback);
    }
}
