using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RAG.Abstractions.Search;
using RAG.Orchestrator.Api.Features.Search;
using RAG.Orchestrator.Api.Features.Search.DocumentReconstruction;
using RAG.Orchestrator.Api.Features.Search.ResultMapping;
using System.Text.Json;

namespace RAG.Tests.Orchestrator;

public class ResultMapperTests
{
    private readonly Mock<IDocumentReconstructor> _mockReconstructor;
    private readonly Mock<ILogger<ResultMapper>> _mockLogger;
    private readonly ResultMapper _mapper;

    public ResultMapperTests()
    {
        _mockReconstructor = new Mock<IDocumentReconstructor>();
        _mockLogger = new Mock<ILogger<ResultMapper>>();
        _mapper = new ResultMapper(_mockReconstructor.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task MapSearchResponseAsync_WithEmptyResults_ReturnsEmptyResponse()
    {
        // Arrange
        var request = new SearchRequest("test", null, 10, 0);
        var elasticsearchResponse = JsonSerializer.Serialize(new
        {
            hits = new
            {
                total = new { value = 0 },
                hits = Array.Empty<object>()
            },
            took = 10
        });

        // Act
        var result = await _mapper.MapSearchResponseAsync(elasticsearchResponse, request);

        // Assert
        result.Results.Should().BeEmpty();
        result.Total.Should().Be(0);
        result.Query.Should().Be("test");
    }

    [Fact]
    public async Task MapSearchResponseAsync_WithSingleChunk_MapsCorrectly()
    {
        // Arrange
        var request = new SearchRequest("test", null, 10, 0);
        var elasticsearchResponse = JsonSerializer.Serialize(new
        {
            hits = new
            {
                total = new { value = 1 },
                hits = new[]
                {
                    new
                    {
                        _id = "chunk1",
                        _score = 0.95,
                        _source = new
                        {
                            content = "Test content",
                            sourceFile = "test.pdf",
                            position = new { chunkIndex = 0, totalChunks = 1 }
                        },
                        highlight = new
                        {
                            content = new[] { "<em>Test</em> content" }
                        }
                    }
                }
            },
            took = 15
        });

        _mockReconstructor.Setup(r => r.ShouldReconstructFullDocument(It.IsAny<List<ChunkInfo>>()))
            .Returns(false);

        // Act
        var result = await _mapper.MapSearchResponseAsync(elasticsearchResponse, request);

        // Assert
        result.Results.Should().HaveCount(1);
        result.Total.Should().Be(1);
        result.Results[0].Content.Should().Contain("Test content");
    }

    [Fact]
    public async Task MapSearchResponseAsync_WithMultipleChunksFromSameFile_ReconstructsDocument()
    {
        // Arrange
        var request = new SearchRequest("test", null, 10, 0);
        var elasticsearchResponse = JsonSerializer.Serialize(new
        {
            hits = new
            {
                total = new { value = 3 },
                hits = new[]
                {
                    new
                    {
                        _id = "chunk1",
                        _score = 0.95,
                        _source = new
                        {
                            content = "First chunk",
                            sourceFile = "test.pdf",
                            position = new { chunkIndex = 0, totalChunks = 3 }
                        }
                    },
                    new
                    {
                        _id = "chunk2",
                        _score = 0.90,
                        _source = new
                        {
                            content = "Second chunk",
                            sourceFile = "test.pdf",
                            position = new { chunkIndex = 1, totalChunks = 3 }
                        }
                    },
                    new
                    {
                        _id = "chunk3",
                        _score = 0.85,
                        _source = new
                        {
                            content = "Third chunk",
                            sourceFile = "test.pdf",
                            position = new { chunkIndex = 2, totalChunks = 3 }
                        }
                    }
                }
            },
            took = 20
        });

        var reconstructedResult = new SearchResult(
            "reconstructed",
            "Reconstructed Document",
            "First chunk Second chunk Third chunk",
            0.95,
            "test.pdf",
            "pdf",
            null,
            "test.pdf",
            new Dictionary<string, object>(),
            DateTime.UtcNow,
            DateTime.UtcNow
        );

        _mockReconstructor.Setup(r => r.ShouldReconstructFullDocument(It.IsAny<List<ChunkInfo>>()))
            .Returns(true);
        _mockReconstructor.Setup(r => r.ReconstructDocumentFromChunksAsync(
                "test.pdf",
                It.IsAny<List<ChunkInfo>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reconstructedResult);

        // Act
        var result = await _mapper.MapSearchResponseAsync(elasticsearchResponse, request);

        // Assert
        result.Results.Should().HaveCount(1);
        result.Results[0].Content.Should().Contain("First chunk Second chunk Third chunk");
        result.Results[0].Title.Should().Be("Reconstructed Document");
        _mockReconstructor.Verify(r => r.ReconstructDocumentFromChunksAsync(
            "test.pdf",
            It.IsAny<List<ChunkInfo>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MapSearchResponseAsync_WithChunksFromDifferentFiles_ReturnsMultipleResults()
    {
        // Arrange
        var request = new SearchRequest("test", null, 10, 0);
        var elasticsearchResponse = JsonSerializer.Serialize(new
        {
            hits = new
            {
                total = new { value = 2 },
                hits = new[]
                {
                    new
                    {
                        _id = "chunk1",
                        _score = 0.95,
                        _source = new
                        {
                            content = "Content from file1",
                            sourceFile = "file1.pdf",
                            position = new { chunkIndex = 0, totalChunks = 1 }
                        }
                    },
                    new
                    {
                        _id = "chunk2",
                        _score = 0.90,
                        _source = new
                        {
                            content = "Content from file2",
                            sourceFile = "file2.pdf",
                            position = new { chunkIndex = 0, totalChunks = 1 }
                        }
                    }
                }
            },
            took = 15
        });

        _mockReconstructor.Setup(r => r.ShouldReconstructFullDocument(It.IsAny<List<ChunkInfo>>()))
            .Returns(false);

        // Act
        var result = await _mapper.MapSearchResponseAsync(elasticsearchResponse, request);

        // Assert
        result.Results.Should().HaveCount(2);
        result.Results[0].Source.Should().Be("file1.pdf");
        result.Results[1].Source.Should().Be("file2.pdf");
    }

    [Fact]
    public async Task MapSearchResponseAsync_RespectsLimit()
    {
        // Arrange
        var request = new SearchRequest("test", null, 2, 0);
        var elasticsearchResponse = JsonSerializer.Serialize(new
        {
            hits = new
            {
                total = new { value = 5 },
                hits = new[]
                {
                    new { _id = "chunk1", _score = 0.95, _source = new { content = "Content 1", sourceFile = "file1.pdf", position = new { chunkIndex = 0, totalChunks = 1 } } },
                    new { _id = "chunk2", _score = 0.90, _source = new { content = "Content 2", sourceFile = "file2.pdf", position = new { chunkIndex = 0, totalChunks = 1 } } },
                    new { _id = "chunk3", _score = 0.85, _source = new { content = "Content 3", sourceFile = "file3.pdf", position = new { chunkIndex = 0, totalChunks = 1 } } }
                }
            },
            took = 10
        });

        _mockReconstructor.Setup(r => r.ShouldReconstructFullDocument(It.IsAny<List<ChunkInfo>>()))
            .Returns(false);

        // Act
        var result = await _mapper.MapSearchResponseAsync(elasticsearchResponse, request);

        // Assert
        result.Results.Should().HaveCount(2);
        result.Total.Should().Be(5); // Total hits includes all, but results are limited
    }

    [Fact]
    public void MapChunkInfo_WithFullData_MapsAllProperties()
    {
        // Arrange
        var hitJson = JsonSerializer.Serialize(new
        {
            _id = "chunk123",
            _score = 0.95,
            _source = new
            {
                content = "Test content",
                sourceFile = "test.pdf",
                position = new
                {
                    chunkIndex = 2,
                    totalChunks = 5,
                    startIndex = 100,
                    endIndex = 200
                },
                fileExtension = ".pdf",
                indexedAt = "2024-01-01T00:00:00Z"
            },
            highlight = new
            {
                content = new[] { "<em>Test</em> content", "Another <em>test</em>" }
            }
        });

        var hit = JsonSerializer.Deserialize<JsonElement>(hitJson);

        // Act
        var result = _mapper.MapChunkInfo(hit);

        // Assert
        result.Id.Should().Be("chunk123");
        result.Score.Should().Be(0.95);
        result.Content.Should().Be("Test content");
        result.SourceFile.Should().Be("test.pdf");
        result.ChunkIndex.Should().Be(2);
        result.TotalChunks.Should().Be(5);
        result.Highlights.Should().HaveCount(2);
        result.Highlights.Should().Contain("<em>Test</em> content");
    }

    [Fact]
    public void MapChunkInfo_WithoutHighlights_MapsWithoutHighlights()
    {
        // Arrange
        var hitJson = JsonSerializer.Serialize(new
        {
            _id = "chunk1",
            _score = 0.8,
            _source = new
            {
                content = "Content without highlights",
                sourceFile = "test.pdf",
                position = new { chunkIndex = 0, totalChunks = 1 }
            }
        });

        var hit = JsonSerializer.Deserialize<JsonElement>(hitJson);

        // Act
        var result = _mapper.MapChunkInfo(hit);

        // Assert
        result.Highlights.Should().BeEmpty();
    }

    [Fact]
    public void MapChunkInfo_WithEmptyContent_HandlesGracefully()
    {
        // Arrange
        var hitJson = JsonSerializer.Serialize(new
        {
            _id = "chunk1",
            _score = 0.5,
            _source = new
            {
                content = "",
                sourceFile = "test.pdf",
                position = new { chunkIndex = 0, totalChunks = 1 }
            }
        });

        var hit = JsonSerializer.Deserialize<JsonElement>(hitJson);

        // Act
        var result = _mapper.MapChunkInfo(hit);

        // Assert
        result.Content.Should().BeEmpty();
        result.SourceFile.Should().Be("test.pdf");
    }

    [Fact]
    public async Task MapSearchResponseAsync_WithChunksExceedingLimit_StopsProcessing()
    {
        // Arrange
        var request = new SearchRequest("test", null, 1, 0);
        var elasticsearchResponse = JsonSerializer.Serialize(new
        {
            hits = new
            {
                total = new { value = 10 },
                hits = new[]
                {
                    new { _id = "chunk1", _score = 0.95, _source = new { content = "Content 1", sourceFile = "file1.pdf", position = new { chunkIndex = 0, totalChunks = 1 } } },
                    new { _id = "chunk2", _score = 0.90, _source = new { content = "Content 2", sourceFile = "file2.pdf", position = new { chunkIndex = 0, totalChunks = 1 } } }
                }
            },
            took = 10
        });

        _mockReconstructor.Setup(r => r.ShouldReconstructFullDocument(It.IsAny<List<ChunkInfo>>()))
            .Returns(false);

        // Act
        var result = await _mapper.MapSearchResponseAsync(elasticsearchResponse, request);

        // Assert
        result.Results.Should().HaveCount(1);
    }
}

