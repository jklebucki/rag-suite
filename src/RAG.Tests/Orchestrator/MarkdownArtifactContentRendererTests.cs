using DocumentFormat.OpenXml.Packaging;
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
    public void Render_TxtUsesUtf8WithoutBom()
    {
        var renderer = new MarkdownArtifactContentRenderer();

        var content = renderer.Render(GeneratedArtifactFormat.Txt, "Zażółć gęślą jaźń");

        Assert.Equal("Zażółć gęślą jaźń", System.Text.Encoding.UTF8.GetString(content));
        Assert.False(content.AsSpan().StartsWith(System.Text.Encoding.UTF8.Preamble));
    }
}
