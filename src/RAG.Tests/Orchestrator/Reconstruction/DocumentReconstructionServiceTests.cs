using RAG.Orchestrator.Api.Features.Reconstruction;
using RAG.Abstractions.Search;
using RAG.Orchestrator.Api.Features.Search;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace RAG.Tests.Orchestrator.Reconstruction;

public class DocumentReconstructionServiceTests
{
    private readonly DocumentReconstructionService _service;
    private readonly Mock<ILogger<DocumentReconstructionService>> _loggerMock;

    public DocumentReconstructionServiceTests()
    {
        _loggerMock = new Mock<ILogger<DocumentReconstructionService>>();
        _service = new DocumentReconstructionService(_loggerMock.Object);
    }

    [Fact]
    public void ReconstructDocument_WithSingleChunk_ReturnsOriginalContent()
    {
        // Arrange
        var chunks = new List<ChunkInfo>
        {
            new ChunkInfo
            {
                ChunkIndex = 0,
                Content = "This is a single chunk document."
            }
        };

        // Act
        var result = _service.ReconstructDocument(chunks);

        // Assert
        Assert.Equal("This is a single chunk document.", result);
    }

    [Fact]
    public void ReconstructDocument_WithoutOverlap_ConcatenatesChunks()
    {
        // Arrange
        var chunks = new List<ChunkInfo>
        {
            new ChunkInfo
            {
                ChunkIndex = 0,
                Content = "First chunk content."
            },
            new ChunkInfo
            {
                ChunkIndex = 1,
                Content = "Second chunk content."
            }
        };

        // Act
        var result = _service.ReconstructDocument(chunks, removeOverlap: false);

        // Assert
        Assert.Equal("First chunk content.\n\nSecond chunk content.", result);
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

        // Assert
        Assert.DoesNotContain("shared content at the end. shared content at the end.", result);
        Assert.Contains("This is the first chunk with some shared content at the end. This is the second chunk.", result);
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

        // Assert
        Assert.DoesNotContain("Third sentence with details. Third sentence with details.", result);
        Assert.Contains("Third sentence with details. Fourth sentence continues the text.", result);
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

        // Assert
        Assert.DoesNotContain("topics and procedures topics and procedures", result);
        Assert.Contains("This document discusses important topics and procedures for handling data processing tasks", result);
    }

    [Fact]
    public void ReconstructDocument_WithEmptyChunks_SkipsEmptyChunks()
    {
        // Arrange
        var chunks = new List<ChunkInfo>
        {
            new ChunkInfo
            {
                ChunkIndex = 0,
                Content = "First chunk with content."
            },
            new ChunkInfo
            {
                ChunkIndex = 1,
                Content = ""
            },
            new ChunkInfo
            {
                ChunkIndex = 2,
                Content = "Third chunk with content."
            }
        };

        // Act
        var result = _service.ReconstructDocument(chunks);

        // Assert
        Assert.Equal("First chunk with content. Third chunk with content.", result);
    }

    [Fact]
    public void ReconstructDocument_WithUnorderedChunks_SortsChunksByIndex()
    {
        // Arrange
        var chunks = new List<ChunkInfo>
        {
            new ChunkInfo
            {
                ChunkIndex = 2,
                Content = "Third chunk."
            },
            new ChunkInfo
            {
                ChunkIndex = 0,
                Content = "First chunk."
            },
            new ChunkInfo
            {
                ChunkIndex = 1,
                Content = "Second chunk."
            }
        };

        // Act
        var result = _service.ReconstructDocument(chunks);

        // Assert
        Assert.StartsWith("First chunk.", result);
        Assert.Contains("Second chunk.", result);
        Assert.EndsWith("Third chunk.", result);
    }

    [Fact]
    public void RemoveOverlap_WithExactMatch_RemovesOverlap()
    {
        // Arrange
        var previousText = "This is some text with overlapping content";
        var currentText = "overlapping content continues here";

        // Act
        var result = _service.RemoveOverlap(previousText, currentText, 200);

        // Assert
        Assert.Equal(" continues here", result);
    }

    [Fact]
    public void RemoveOverlap_WithNoOverlap_ReturnsOriginalText()
    {
        // Arrange
        var previousText = "This is some text";
        var currentText = "This is completely different text";

        // Act
        var result = _service.RemoveOverlap(previousText, currentText, 200);

        // Assert
        Assert.Equal("This is completely different text", result);
    }

    [Fact]
    public void RemoveOverlap_WithEmptyPreviousText_ReturnsCurrentText()
    {
        // Arrange
        var previousText = "";
        var currentText = "This is the current text";

        // Act
        var result = _service.RemoveOverlap(previousText, currentText, 200);

        // Assert
        Assert.Equal("This is the current text", result);
    }

    [Theory]
    [InlineData("PAGEREF _Toc164694248", "PAGEREF _Toc164694248", 20)] // Exact match
    [InlineData("end of chunk", "end of chunk continues", 15)] // Partial match
    [InlineData("no match here", "different content", 20)] // No match
    public void RemoveOverlap_WithVariousInputs_HandlesCorrectly(string previous, string current, int maxLength)
    {
        // Act
        var result = _service.RemoveOverlap(previous, current, maxLength);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length <= current.Length);
    }
}
