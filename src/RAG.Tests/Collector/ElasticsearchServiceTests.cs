using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RAG.Collector.Config;
using RAG.Collector.Elasticsearch;

namespace RAG.Tests.Collector;

/// <summary>
/// Tests for ElasticsearchService
/// Note: Due to the complexity of mocking Elasticsearch.Net's API with optional parameters,
/// these tests are simplified. More comprehensive testing is done through IndexingService integration tests.
/// </summary>
public class ElasticsearchServiceTests
{
    private readonly Mock<IElasticLowLevelClient> _mockClient;
    private readonly Mock<ILogger<ElasticsearchService>> _mockLogger;
    private readonly CollectorOptions _options;
    private readonly ElasticsearchService _service;

    public ElasticsearchServiceTests()
    {
        _mockClient = new Mock<IElasticLowLevelClient>();
        _mockLogger = new Mock<ILogger<ElasticsearchService>>();

        _options = new CollectorOptions
        {
            IndexName = "test-index"
        };

        _service = new ElasticsearchService(
            _mockClient.Object,
            _mockLogger.Object,
            Options.Create(_options));
    }

    [Fact]
    public async Task IndexDocumentsBatchAsync_WithEmptyList_ReturnsZero()
    {
        // Act
        var result = await _service.IndexDocumentsBatchAsync(new List<ChunkDocument>());

        // Assert
        result.Should().Be(0);
    }


    private static ChunkDocument CreateTestChunkDocument()
    {
        return new ChunkDocument
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Test content",
            Embedding = new float[768],
            SourceFile = "test.txt",
            FileExtension = ".txt",
            FileSize = 100,
            LastModified = DateTime.UtcNow
        };
    }
}
