using RAG.DocumentProcessing.Abstractions;
using RAG.Orchestrator.Api.Features.Chat.Artifacts;

namespace RAG.Tests.Orchestrator;

public class GeneratedArtifactBlockParserTests
{
    [Fact]
    public void Extract_RemovesArtifactBlockFromVisibleResponse()
    {
        var block = GeneratedArtifactBlockParser.Extract(
            "Done.\n<generated_artifact format=\"docx\" filename=\"report.docx\"># Report</generated_artifact>",
            out var cleanResponse);

        Assert.NotNull(block);
        Assert.Equal(GeneratedArtifactFormat.Docx, block.Format);
        Assert.Equal("report.docx", block.FileName);
        Assert.Equal("# Report", block.Markdown);
        Assert.Equal("Done.", cleanResponse);
    }

    [Fact]
    public void Sanitize_RejectsPathTraversal()
    {
        var exception = Assert.Throws<DocumentProcessingException>(() =>
            ArtifactFileNameSanitizer.Sanitize("../secrets.docx", GeneratedArtifactFormat.Docx));

        Assert.Equal("INVALID_ARTIFACT_FILE_NAME", exception.Code);
    }
}
