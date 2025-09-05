using RAG.Orchestrator.Api.Features.Reconstruction;
using RAG.Orchestrator.Api.Features.Search;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace RAG.Orchestrator.Api.Tests.Features.Reconstruction;

public class DebugDocumentReconstructionTests
{
    private readonly DocumentReconstructionService _service;
    private readonly Mock<ILogger<DocumentReconstructionService>> _loggerMock;
    private readonly ITestOutputHelper _output;

    public DebugDocumentReconstructionTests(ITestOutputHelper output)
    {
        _loggerMock = new Mock<ILogger<DocumentReconstructionService>>();
        _service = new DocumentReconstructionService(_loggerMock.Object);
        _output = output;
    }

    [Fact]
    public void Debug_SimpleOverlap_ShowsWhatHappens()
    {
        // Arrange
        var previousText = "This is the first chunk with some shared content at the end.";
        var currentText = "shared content at the end. This is the second chunk.";

        // Act
        var result = _service.RemoveOverlap(previousText, currentText, 200);
        
        // Debug output
        _output.WriteLine($"Previous text: '{previousText}'");
        _output.WriteLine($"Current text: '{currentText}'");
        _output.WriteLine($"Result: '{result}'");
        _output.WriteLine($"Expected overlap: 'shared content at the end.'");

        // Assert - najpierw sprawdźmy czy w ogóle coś zostało usunięte
        Assert.True(result.Length < currentText.Length, $"Expected result length ({result.Length}) to be less than original ({currentText.Length})");
    }

    [Fact]
    public void Debug_ExactMatch_AtBeginning()
    {
        // Arrange
        var previousText = "End with exact match";
        var currentText = "exact match starts here";

        // Act
        var result = _service.RemoveOverlap(previousText, currentText, 200);
        
        _output.WriteLine($"Previous: '{previousText}'");
        _output.WriteLine($"Current: '{currentText}'");
        _output.WriteLine($"Result: '{result}'");

        // Expected: " starts here"
        Assert.Equal(" starts here", result);
    }

    [Fact]
    public void Debug_ReconstructTwoChunks()
    {
        // Arrange
        var chunks = new List<ChunkInfo>
        {
            new ChunkInfo
            {
                ChunkIndex = 0,
                Content = "First chunk content with overlap"
            },
            new ChunkInfo
            {
                ChunkIndex = 1,
                Content = "with overlap and new content"
            }
        };

        // Act
        var result = _service.ReconstructDocument(chunks);
        
        _output.WriteLine($"Chunk 1: '{chunks[0].Content}'");
        _output.WriteLine($"Chunk 2: '{chunks[1].Content}'");
        _output.WriteLine($"Result: '{result}'");

        // Basic sanity check
        Assert.NotEmpty(result);
        Assert.Contains("First chunk content", result);
        Assert.Contains("new content", result);
    }
}
