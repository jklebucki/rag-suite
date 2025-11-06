using Xunit;
using RAG.Collector.Indexing;
using RAG.Collector.Elasticsearch;
using RAG.Collector.Embeddings;
using RAG.Collector.Config;
using RAG.Collector.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;

namespace RAG.Tests.Collector;

public class IndexingServiceTests
{
    private readonly Mock<IEmbeddingProvider> _mockEmbeddingProvider;
    private readonly Mock<IElasticsearchService> _mockElasticsearchService;
    private readonly Mock<IFileChangeDetectionService> _mockFileChangeDetection;
    private readonly Mock<ILogger<IndexingService>> _mockLogger;
    private readonly CollectorOptions _options;
    private readonly IndexingService _service;

    public IndexingServiceTests()
    {
        _mockEmbeddingProvider = new Mock<IEmbeddingProvider>();
        _mockElasticsearchService = new Mock<IElasticsearchService>();
        _mockFileChangeDetection = new Mock<IFileChangeDetectionService>();
        _mockLogger = new Mock<ILogger<IndexingService>>();

        _options = new CollectorOptions
        {
            BulkBatchSize = 200,
            IndexName = "test-index"
        };

        _mockEmbeddingProvider.Setup(x => x.ModelName).Returns("test-model");
        _mockEmbeddingProvider.Setup(x => x.VectorDimensions).Returns(768);

        _service = new IndexingService(
            _mockEmbeddingProvider.Object,
            _mockElasticsearchService.Object,
            _mockFileChangeDetection.Object,
            _mockLogger.Object,
            Options.Create(_options));
    }

    [Fact]
    public async Task IndexChunkAsync_WithValidChunk_ReturnsTrue()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var embedding = new float[768];
        var embeddingResult = EmbeddingResult.CreateSuccess(embedding, "test-model", 100, TimeSpan.FromMilliseconds(50));

        _mockEmbeddingProvider
            .Setup(x => x.GenerateEmbeddingAsync(chunk, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingResult);

        _mockElasticsearchService
            .Setup(x => x.IndexDocumentAsync(It.IsAny<ChunkDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.IndexChunkAsync(chunk);

        // Assert
        result.Should().BeTrue();
        _mockEmbeddingProvider.Verify(x => x.GenerateEmbeddingAsync(chunk, It.IsAny<CancellationToken>()), Times.Once);
        _mockElasticsearchService.Verify(x => x.IndexDocumentAsync(It.IsAny<ChunkDocument>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IndexChunkAsync_WithEmbeddingFailure_ReturnsFalse()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var embeddingResult = EmbeddingResult.CreateFailure("Embedding failed");

        _mockEmbeddingProvider
            .Setup(x => x.GenerateEmbeddingAsync(chunk, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingResult);

        // Act
        var result = await _service.IndexChunkAsync(chunk);

        // Assert
        result.Should().BeFalse();
        _mockElasticsearchService.Verify(x => x.IndexDocumentAsync(It.IsAny<ChunkDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IndexChunkAsync_WithElasticsearchFailure_ReturnsFalse()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var embedding = new float[768];
        var embeddingResult = EmbeddingResult.CreateSuccess(embedding, "test-model", 100, TimeSpan.FromMilliseconds(50));

        _mockEmbeddingProvider
            .Setup(x => x.GenerateEmbeddingAsync(chunk, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingResult);

        _mockElasticsearchService
            .Setup(x => x.IndexDocumentAsync(It.IsAny<ChunkDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.IndexChunkAsync(chunk);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IndexChunksBatchAsync_WithEmptyList_ReturnsZero()
    {
        // Act
        var result = await _service.IndexChunksBatchAsync(new List<TextChunk>());

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task IndexChunksBatchAsync_WithValidChunks_ReturnsCount()
    {
        // Arrange
        var chunks = new List<TextChunk> { CreateTestChunk(), CreateTestChunk() };
        var embedding = new float[768];
        var embeddingResults = new List<EmbeddingResult>
        {
            EmbeddingResult.CreateSuccess(embedding, "test-model", 100, TimeSpan.FromMilliseconds(50)),
            EmbeddingResult.CreateSuccess(embedding, "test-model", 100, TimeSpan.FromMilliseconds(50))
        };

        _mockEmbeddingProvider
            .Setup(x => x.GenerateBatchEmbeddingsAsync(chunks, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingResults);

        _mockElasticsearchService
            .Setup(x => x.IndexDocumentsBatchAsync(It.IsAny<IList<ChunkDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var result = await _service.IndexChunksBatchAsync(chunks);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task IndexChunksBatchAsync_WithPartialEmbeddingFailures_IndexesSuccessfulOnes()
    {
        // Arrange
        var chunks = new List<TextChunk> { CreateTestChunk(), CreateTestChunk() };
        var embedding = new float[768];
        var embeddingResults = new List<EmbeddingResult>
        {
            EmbeddingResult.CreateSuccess(embedding, "test-model", 100, TimeSpan.FromMilliseconds(50)),
            EmbeddingResult.CreateFailure("Failed")
        };

        _mockEmbeddingProvider
            .Setup(x => x.GenerateBatchEmbeddingsAsync(chunks, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingResults);

        _mockElasticsearchService
            .Setup(x => x.IndexDocumentsBatchAsync(It.IsAny<IList<ChunkDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.IndexChunksBatchAsync(chunks);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task IndexFileChunksAsync_WithEmptyList_ReturnsZero()
    {
        // Act
        var result = await _service.IndexFileChunksAsync(new List<TextChunk>());

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task IndexFileChunksAsync_WithUnchangedFile_SkipsIndexing()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var chunks = new List<TextChunk> { chunk };

        _mockFileChangeDetection
            .Setup(x => x.ShouldReindexFileAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.IndexFileChunksAsync(chunks);

        // Assert
        result.Should().Be(0);
        _mockElasticsearchService.Verify(x => x.IndexDocumentsBatchAsync(It.IsAny<IList<ChunkDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IndexFileChunksAsync_WithChangedFile_IndexesChunks()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var chunks = new List<TextChunk> { chunk };
        var embedding = new float[768];
        var embeddingResults = new List<EmbeddingResult>
        {
            EmbeddingResult.CreateSuccess(embedding, "test-model", 100, TimeSpan.FromMilliseconds(50))
        };

        _mockFileChangeDetection
            .Setup(x => x.ShouldReindexFileAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockElasticsearchService
            .Setup(x => x.DeleteDocumentsBySourceFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockEmbeddingProvider
            .Setup(x => x.GenerateBatchEmbeddingsAsync(chunks, It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddingResults);

        _mockElasticsearchService
            .Setup(x => x.IndexDocumentsBatchAsync(It.IsAny<IList<ChunkDocument>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.IndexFileChunksAsync(chunks);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task EnsureReadyAsync_WithAllServicesAvailable_ReturnsTrue()
    {
        // Arrange
        _mockEmbeddingProvider
            .Setup(x => x.IsAvailableAsync())
            .ReturnsAsync(true);

        _mockElasticsearchService
            .Setup(x => x.IsAvailableAsync())
            .ReturnsAsync(true);

        _mockElasticsearchService
            .Setup(x => x.EnsureIndexExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.EnsureReadyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureReadyAsync_WithEmbeddingUnavailable_ReturnsFalse()
    {
        // Arrange
        _mockEmbeddingProvider
            .Setup(x => x.IsAvailableAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _service.EnsureReadyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EnsureReadyAsync_WithElasticsearchUnavailable_ReturnsFalse()
    {
        // Arrange
        _mockEmbeddingProvider
            .Setup(x => x.IsAvailableAsync())
            .ReturnsAsync(true);

        _mockElasticsearchService
            .Setup(x => x.IsAvailableAsync())
            .ReturnsAsync(false);

        // Act
        var result = await _service.EnsureReadyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetStatsAsync_WithValidStats_ReturnsStats()
    {
        // Arrange
        var indexStats = new IndexStats
        {
            DocumentCount = 100,
            IndexSizeBytes = 1024,
            IndexName = "test-index",
            LastUpdated = DateTime.UtcNow
        };

        _mockElasticsearchService
            .Setup(x => x.GetIndexStatsAsync())
            .ReturnsAsync(indexStats);

        // Act
        var result = await _service.GetStatsAsync();

        // Assert
        result.Should().NotBeNull();
        result!.TotalDocuments.Should().Be(100);
        result.IndexSizeBytes.Should().Be(1024);
        result.EmbeddingModel.Should().Be("test-model");
        result.VectorDimensions.Should().Be(768);
    }

    private static TextChunk CreateTestChunk()
    {
        var fileItem = new FileItem
        {
            Path = "test.txt",
            Extension = ".txt",
            Size = 100,
            LastWriteTimeUtc = DateTime.UtcNow
        };

        return new TextChunk
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Test content",
            SourceFile = fileItem,
            FileHash = "test-hash",
            Position = new ChunkPosition
            {
                StartIndex = 0,
                EndIndex = 12,
                ChunkIndex = 0,
                TotalChunks = 1
            }
        };
    }
}

