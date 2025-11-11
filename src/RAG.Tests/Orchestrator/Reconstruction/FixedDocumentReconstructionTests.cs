using Microsoft.Extensions.Logging;
using Moq;
using RAG.Orchestrator.Api.Features.Reconstruction;
using RAG.Orchestrator.Api.Features.Search;
using Xunit.Abstractions;

namespace RAG.Tests.Orchestrator.Reconstruction;

public class FixedDocumentReconstructionTests
{
    private readonly DocumentReconstructionService _service;
    private readonly Mock<ILogger<DocumentReconstructionService>> _loggerMock;
    private readonly ITestOutputHelper _output;

    public FixedDocumentReconstructionTests(ITestOutputHelper output)
    {
        _loggerMock = new Mock<ILogger<DocumentReconstructionService>>();
        _service = new DocumentReconstructionService(_loggerMock.Object);
        _output = output;
    }

    [Fact]
    public void ReconstructDocument_WithExactOverlap_RemovesOverlap()
    {
        // Arrange
        var chunks = new List<ChunkInfo>
        {
            new ChunkInfo
            {
                ChunkIndex = 0,
                Content = "This is the first chunk with some shared content at the end."
            },
            new ChunkInfo
            {
                ChunkIndex = 1,
                Content = "shared content at the end. This is the second chunk."
            }
        };

        // Act
        var result = _service.ReconstructDocument(chunks);

        _output.WriteLine($"Result: '{result}'");

        // Assert - sprawdzamy czy nie ma duplikacji, ale nie wymuszamy konkretnego formatu
        Assert.DoesNotContain("shared content at the end. shared content at the end.", result);
        Assert.Contains("This is the first chunk", result);
        Assert.Contains("This is the second chunk", result);

        // Sprawdź czy długość jest rozsądna (mniejsza niż suma, ale większa niż tylko jeden chunk)
        var totalLength = chunks.Sum(c => c.Content?.Length ?? 0);
        Assert.True(result.Length < totalLength, "Result should be shorter than sum of all chunks");
        Assert.True(result.Length > chunks[0].Content!.Length, "Result should be longer than single chunk");
    }

    [Fact]
    public void ReconstructDocument_WithWordOverlap_RemovesOverlapAtWordBoundary()
    {
        // Arrange
        var chunks = new List<ChunkInfo>
        {
            new ChunkInfo
            {
                ChunkIndex = 0,
                Content = "This document discusses important topics and procedures"
            },
            new ChunkInfo
            {
                ChunkIndex = 1,
                Content = "topics and procedures for handling data processing tasks"
            }
        };

        // Act
        var result = _service.ReconstructDocument(chunks);

        _output.WriteLine($"Result: '{result}'");

        // Assert - główne sprawdzenie to brak duplikacji
        Assert.DoesNotContain("topics and procedures topics and procedures", result);
        Assert.Contains("This document discusses", result);
        Assert.Contains("data processing tasks", result);
    }

    [Fact]
    public void ReconstructDocument_WithSentenceOverlap_RemovesOverlapAtSentenceBoundary()
    {
        // Arrange
        var chunks = new List<ChunkInfo>
        {
            new ChunkInfo
            {
                ChunkIndex = 0,
                Content = "First paragraph. Second sentence here. Third sentence with details."
            },
            new ChunkInfo
            {
                ChunkIndex = 1,
                Content = "Third sentence with details. Fourth sentence continues the text."
            }
        };

        // Act
        var result = _service.ReconstructDocument(chunks);

        _output.WriteLine($"Result: '{result}'");

        // Assert
        Assert.DoesNotContain("Third sentence with details. Third sentence with details.", result);
        Assert.Contains("First paragraph", result);
        Assert.Contains("Fourth sentence continues", result);
    }
}
