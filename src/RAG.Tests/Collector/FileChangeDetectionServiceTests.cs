using Xunit;
using RAG.Collector.Indexing;
using RAG.Collector.Elasticsearch;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace RAG.Tests.Collector;

public class FileChangeDetectionServiceTests
{
    private readonly Mock<IElasticsearchService> _mockElasticsearchService;
    private readonly Mock<ILogger<FileChangeDetectionService>> _mockLogger;
    private readonly FileChangeDetectionService _service;

    public FileChangeDetectionServiceTests()
    {
        _mockElasticsearchService = new Mock<IElasticsearchService>();
        _mockLogger = new Mock<ILogger<FileChangeDetectionService>>();

        _service = new FileChangeDetectionService(
            _mockElasticsearchService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ShouldReindexFileAsync_WithNoExistingMetadata_ReturnsTrue()
    {
        // Arrange
        var filePath = "test.txt";
        var fileHash = "hash123";
        var lastModified = DateTime.UtcNow;

        _mockElasticsearchService
            .Setup(x => x.EnsureCustomIndexExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockElasticsearchService
            .Setup(x => x.GetDocumentByIdAsync<FileMetadataDocument>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileMetadataDocument?)null);

        // Act
        var result = await _service.ShouldReindexFileAsync(filePath, fileHash, lastModified);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldReindexFileAsync_WithMatchingHashAndDate_ReturnsFalse()
    {
        // Arrange
        var filePath = "test.txt";
        var fileHash = "hash123";
        var lastModified = DateTime.UtcNow;

        var existingMetadata = new FileMetadataDocument
        {
            Id = "test-id",
            FilePath = filePath,
            ContentHash = fileHash,
            LastModified = lastModified,
            ChunkCount = 5,
            IndexedAt = DateTime.UtcNow,
            FileExtension = ".txt"
        };

        _mockElasticsearchService
            .Setup(x => x.EnsureCustomIndexExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockElasticsearchService
            .Setup(x => x.GetDocumentByIdAsync<FileMetadataDocument>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMetadata);

        // Act
        var result = await _service.ShouldReindexFileAsync(filePath, fileHash, lastModified);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldReindexFileAsync_WithDifferentHash_ReturnsTrue()
    {
        // Arrange
        var filePath = "test.txt";
        var fileHash = "hash456";
        var lastModified = DateTime.UtcNow;

        var existingMetadata = new FileMetadataDocument
        {
            Id = "test-id",
            FilePath = filePath,
            ContentHash = "hash123", // Different hash
            LastModified = lastModified,
            ChunkCount = 5,
            IndexedAt = DateTime.UtcNow,
            FileExtension = ".txt"
        };

        _mockElasticsearchService
            .Setup(x => x.EnsureCustomIndexExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockElasticsearchService
            .Setup(x => x.GetDocumentByIdAsync<FileMetadataDocument>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMetadata);

        // Act
        var result = await _service.ShouldReindexFileAsync(filePath, fileHash, lastModified);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldReindexFileAsync_WithDifferentLastModified_ReturnsTrue()
    {
        // Arrange
        var filePath = "test.txt";
        var fileHash = "hash123";
        var lastModified = DateTime.UtcNow;

        var existingMetadata = new FileMetadataDocument
        {
            Id = "test-id",
            FilePath = filePath,
            ContentHash = fileHash,
            LastModified = lastModified.AddDays(-1), // Different date
            ChunkCount = 5,
            IndexedAt = DateTime.UtcNow,
            FileExtension = ".txt"
        };

        _mockElasticsearchService
            .Setup(x => x.EnsureCustomIndexExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockElasticsearchService
            .Setup(x => x.GetDocumentByIdAsync<FileMetadataDocument>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMetadata);

        // Act
        var result = await _service.ShouldReindexFileAsync(filePath, fileHash, lastModified);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldReindexFileAsync_WithException_ReturnsTrue()
    {
        // Arrange
        var filePath = "test.txt";
        var fileHash = "hash123";
        var lastModified = DateTime.UtcNow;

        _mockElasticsearchService
            .Setup(x => x.EnsureCustomIndexExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Elasticsearch error"));

        // Act
        var result = await _service.ShouldReindexFileAsync(filePath, fileHash, lastModified);

        // Assert
        result.Should().BeTrue(); // Defaults to reindex on error
    }

    [Fact]
    public async Task RecordIndexedFileAsync_WithValidData_RecordsMetadata()
    {
        // Arrange
        var filePath = "test.txt";
        var fileHash = "hash123";
        var lastModified = DateTime.UtcNow;
        var chunkCount = 5;

        _mockElasticsearchService
            .Setup(x => x.EnsureCustomIndexExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockElasticsearchService
            .Setup(x => x.IndexDocumentToCustomIndexAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FileMetadataDocument>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.RecordIndexedFileAsync(filePath, fileHash, lastModified, chunkCount);

        // Assert
        _mockElasticsearchService.Verify(
            x => x.IndexDocumentToCustomIndexAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<FileMetadataDocument>(m =>
                    m.FilePath == filePath &&
                    m.ContentHash == fileHash &&
                    m.LastModified == lastModified &&
                    m.ChunkCount == chunkCount),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordIndexedFileAsync_WithException_DoesNotThrow()
    {
        // Arrange
        var filePath = "test.txt";
        var fileHash = "hash123";
        var lastModified = DateTime.UtcNow;
        var chunkCount = 5;

        _mockElasticsearchService
            .Setup(x => x.EnsureCustomIndexExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Error"));

        // Act & Assert
        await _service.RecordIndexedFileAsync(filePath, fileHash, lastModified, chunkCount);
        // Should not throw
    }


    [Fact]
    public async Task DeleteFileMetadataAsync_WithExistingFile_DeletesMetadata()
    {
        // Arrange
        var filePath = "test.txt";

        _mockElasticsearchService
            .Setup(x => x.EnsureCustomIndexExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockElasticsearchService
            .Setup(x => x.DeleteDocumentByIdAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteFileMetadataAsync(filePath);

        // Assert
        result.Should().BeTrue();
        _mockElasticsearchService.Verify(
            x => x.DeleteDocumentByIdAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteFileMetadataAsync_WithException_ReturnsFalse()
    {
        // Arrange
        var filePath = "test.txt";

        _mockElasticsearchService
            .Setup(x => x.EnsureCustomIndexExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Error"));

        // Act
        var result = await _service.DeleteFileMetadataAsync(filePath);

        // Assert
        result.Should().BeFalse();
    }
}

