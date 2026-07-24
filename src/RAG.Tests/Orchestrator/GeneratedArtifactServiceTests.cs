using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RAG.DocumentProcessing.Abstractions;
using RAG.Orchestrator.Api.Features.Chat.Artifacts;

namespace RAG.Tests.Orchestrator;

public class GeneratedArtifactServiceTests
{
    [Fact]
    public async Task ProcessAsync_WhenArtifactIsSaved_AppendsAuthorizedDownloadLinkOnlyAfterSave()
    {
        var renderer = new Mock<IArtifactContentRenderer>();
        renderer.Setup(item => item.Render(GeneratedArtifactFormat.Docx, "# Result")).Returns([1, 2, 3]);
        renderer.Setup(item => item.GetContentType(GeneratedArtifactFormat.Docx)).Returns("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        var store = new Mock<ITemporaryArtifactStore>();
        store.Setup(item => item.SaveAsync(It.IsAny<GeneratedArtifact>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredArtifact("artifact-1", "result.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", 3, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(12)));
        var service = new GeneratedArtifactService(renderer.Object, store.Object, NullLogger<GeneratedArtifactService>.Instance);

        var result = await service.ProcessAsync(
            "Ready.\n<generated_artifact format=\"docx\" filename=\"result.docx\"># Result</generated_artifact>",
            "user-1",
            "session-1",
            "assistant-1",
            CancellationToken.None);

        Assert.True(result.ArtifactCreated);
        Assert.DoesNotContain("<generated_artifact", result.Response, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/api/user-chat/artifacts/artifact-1/download", result.Response);
    }

    [Fact]
    public async Task ProcessAsync_WhenArtifactSaveFails_DoesNotAppendDeadLink()
    {
        var renderer = new Mock<IArtifactContentRenderer>();
        renderer.Setup(item => item.Render(It.IsAny<GeneratedArtifactFormat>(), It.IsAny<string>())).Returns([1]);
        renderer.Setup(item => item.GetContentType(It.IsAny<GeneratedArtifactFormat>())).Returns("text/plain");
        var store = new Mock<ITemporaryArtifactStore>();
        store.Setup(item => item.SaveAsync(It.IsAny<GeneratedArtifact>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk unavailable"));
        var service = new GeneratedArtifactService(renderer.Object, store.Object, NullLogger<GeneratedArtifactService>.Instance);

        var result = await service.ProcessAsync(
            "Text response\n<generated_artifact format=\"txt\" filename=\"result.txt\">payload</generated_artifact>",
            "user-1",
            "session-1",
            "assistant-1",
            CancellationToken.None);

        Assert.False(result.ArtifactCreated);
        Assert.DoesNotContain("/api/user-chat/artifacts/", result.Response, StringComparison.Ordinal);
        Assert.DoesNotContain("<generated_artifact", result.Response, StringComparison.OrdinalIgnoreCase);
    }
}
