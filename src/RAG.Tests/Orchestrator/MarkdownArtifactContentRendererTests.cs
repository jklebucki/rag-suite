using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using RAG.DocumentProcessing.Abstractions;
using RAG.Orchestrator.Api.Features.Chat.Artifacts;

namespace RAG.Tests.Orchestrator;

public class MarkdownArtifactContentRendererTests
{
    [Fact]
    public void Render_DocxCreatesOpenableWordDocumentWithMarkdownContent()
    {
        var renderer = new MarkdownArtifactContentRenderer();

        var content = renderer.Render(GeneratedArtifactFormat.Docx, "# Heading\n\n- First item\n- Second item\n\n| A | B |\n| - | - |\n| 1 | 2 |\n\n```\ncode\n```");

        using var stream = new MemoryStream(content);
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document?.Body;
        Assert.NotNull(body);
        var text = body.InnerText;
        Assert.Contains("Heading", text);
        Assert.Contains("First item", text);
        Assert.Contains("code", text);
    }

    [Fact]
    public void Render_DocxCreatesNativeTableFromMarkdownTable()
    {
        var renderer = new MarkdownArtifactContentRenderer();

        var content = renderer.Render(
            GeneratedArtifactFormat.Docx,
            """
            | Product | License | Limit before | Limit after | Seats before | Seats after |
            | --- | --- | --- | --- | ---: | ---: |
            | Symfonia ERP Finanse i Księgowość | FKF-101358 | VIP warranty | Basic warranty | 36 | 19 |
            | Symfonia Handel | HMF-100790 | VIP warranty | Basic warranty | 13 | 6 |
            """);

        using var stream = new MemoryStream(content);
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document?.Body;
        Assert.NotNull(body);

        var table = Assert.Single(body.Elements<Table>());
        var rows = table.Elements<TableRow>().ToArray();
        Assert.Equal(3, rows.Length);
        Assert.All(rows, row => Assert.Equal(6, row.Elements<TableCell>().Count()));
        Assert.Equal("Product", rows[0].Elements<TableCell>().First().InnerText);
        Assert.Equal("FKF-101358", rows[1].Elements<TableCell>().ElementAt(1).InnerText);
        Assert.Equal("Symfonia Handel", rows[2].Elements<TableCell>().First().InnerText);
        Assert.NotNull(rows[0].Elements<TableCell>().First().Descendants<Run>().Single().RunProperties?.Bold);
    }

    [Fact]
    public void Render_TxtUsesUtf8WithoutBom()
    {
        var renderer = new MarkdownArtifactContentRenderer();

        var content = renderer.Render(GeneratedArtifactFormat.Txt, "Zażółć gęślą jaźń");

        Assert.Equal("Zażółć gęślą jaźń", System.Text.Encoding.UTF8.GetString(content));
        Assert.False(content.AsSpan().StartsWith(System.Text.Encoding.UTF8.Preamble));
    }
}
