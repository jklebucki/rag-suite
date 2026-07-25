using RAG.DocumentProcessing.Abstractions;
using RAG.Orchestrator.Api.Features.Chat.Documents;

namespace RAG.Tests.Orchestrator;

public class ChatDocumentRequestClassifierTests
{
    [Theory]
    [InlineData("Pokaż zawartość dokumentu")]
    [InlineData("Pokaż pełną treść")]
    [InlineData("Show the full document")]
    public void RequestsCanonicalContent_WithExplicitTranscriptionRequest_ReturnsTrue(string message)
    {
        Assert.True(ChatDocumentRequestClassifier.RequestsCanonicalContent(message));
    }

    [Theory]
    [InlineData("Zwróć wynik w DOCX", GeneratedArtifactFormat.Docx)]
    [InlineData("Return a text file", GeneratedArtifactFormat.Txt)]
    public void GetRequestedExportFormat_WithSupportedFormat_ReturnsFormat(string message, GeneratedArtifactFormat expectedFormat)
    {
        Assert.Equal(expectedFormat, ChatDocumentRequestClassifier.GetRequestedExportFormat(message));
    }

    [Fact]
    public void GetRequestedExportFormat_WhenMessageOnlyContainsOrdinaryWord_ReturnsNull()
    {
        Assert.Null(ChatDocumentRequestClassifier.GetRequestedExportFormat("Explain this word from the document."));
    }
}
