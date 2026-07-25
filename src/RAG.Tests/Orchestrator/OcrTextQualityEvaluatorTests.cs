using RAG.DocumentProcessing.Providers.Docling;

namespace RAG.Tests.Orchestrator;

public class OcrTextQualityEvaluatorTests
{
    [Fact]
    public void Calculate_WhenOcrContainsManyOrphanLetters_ReturnsLowQuality()
    {
        const string corrupted = """
            n on sn n n 'zkie prawa do korzystania z programu dotychczas posiadanego
            akt iid i i i i yne przysługują nabywcy wyłącznie na aktualnie posiadany program.
            eid e id s d d e z zd z z zdk rozbudowy aktualnie posiadanego programu.
            """;

        var quality = OcrTextQualityEvaluator.Calculate(corrupted, corrupted, pageCount: 1);

        Assert.True(quality < 0.6d, $"Expected corrupted OCR quality below 0.6 but got {quality:F3}.");
    }

    [Fact]
    public void Calculate_WhenPolishTextIsCoherent_ReturnsHighQuality()
    {
        var coherent = string.Concat(Enumerable.Repeat(
            "Oświadczam, że w związku z dokonaną zmianą przyjmuję do wiadomości wszystkie warunki umowy. ",
            8));

        var quality = OcrTextQualityEvaluator.Calculate(coherent, coherent, pageCount: 1);

        Assert.True(quality >= 0.65d, $"Expected coherent OCR quality at least 0.65 but got {quality:F3}.");
    }
}
