using Xunit;
using RAG.Collector.Embeddings;
using RAG.Collector.Models;
using RAG.Collector.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace RAG.Tests.Collector;

public class HttpEmbeddingProviderTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<HttpEmbeddingProvider>> _mockLogger;
    private readonly CollectorOptions _options;
    private readonly HttpEmbeddingProvider _provider;

    public HttpEmbeddingProviderTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object)
        {
            BaseAddress = new Uri("http://test-embedding-service:8580")
        };

        _mockLogger = new Mock<ILogger<HttpEmbeddingProvider>>();

        _options = new CollectorOptions
        {
            EmbeddingModelName = "test-model",
            EmbeddingServiceUrl = "http://test-embedding-service:8580"
        };

        _provider = new HttpEmbeddingProvider(
            _httpClient,
            _mockLogger.Object,
            Options.Create(_options));
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [Fact]
    public void ModelName_ReturnsConfiguredModelName()
    {
        // Act
        var modelName = _provider.ModelName;

        // Assert
        modelName.Should().Be("test-model");
    }

    [Fact]
    public void VectorDimensions_ReturnsCorrectDimension()
    {
        // Act
        var dimensions = _provider.VectorDimensions;

        // Assert
        dimensions.Should().Be(768);
    }

    [Fact]
    public void MaxTokens_ReturnsCorrectValue()
    {
        // Act
        var maxTokens = _provider.MaxTokens;

        // Assert
        maxTokens.Should().Be(512);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithValidChunk_ReturnsSuccess()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var expectedEmbedding = new float[768];
        for (int i = 0; i < 768; i++)
        {
            expectedEmbedding[i] = (float)(i * 0.001);
        }

        var responseJson = JsonSerializer.Serialize(new[] { expectedEmbedding });
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/embed")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _provider.GenerateEmbeddingAsync(chunk);

        // Assert
        result.Success.Should().BeTrue();
        result.Vector.Should().HaveCount(768);
        result.Vector.Should().BeEquivalentTo(expectedEmbedding);
        result.ModelName.Should().Be("test-model");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithEmptyResponse_ReturnsFailure()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var responseJson = "[]";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _provider.GenerateEmbeddingAsync(chunk);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No embeddings returned");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithHttpError_ReturnsFailure()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error", Encoding.UTF8, "text/plain")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _provider.GenerateEmbeddingAsync(chunk);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("HTTP error");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithPayloadTooLarge_ReturnsFailure()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var response = new HttpResponseMessage((HttpStatusCode)413)
        {
            Content = new StringContent("Payload Too Large", Encoding.UTF8, "text/plain")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _provider.GenerateEmbeddingAsync(chunk);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Payload too large");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithTimeout_ReturnsFailure()
    {
        // Arrange
        var chunk = CreateTestChunk();

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Timeout", new TimeoutException()));

        // Act
        var result = await _provider.GenerateEmbeddingAsync(chunk);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Request timeout");
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithInvalidJson_ReturnsFailure()
    {
        // Arrange
        var chunk = CreateTestChunk();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json", Encoding.UTF8, "application/json")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _provider.GenerateEmbeddingAsync(chunk);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("JSON error");
    }

    [Fact]
    public async Task GenerateBatchEmbeddingsAsync_WithMultipleChunks_ProcessesAll()
    {
        // Arrange
        var chunks = new List<TextChunk> { CreateTestChunk(), CreateTestChunk() };
        var expectedEmbedding = new float[768];
        var responseJson = JsonSerializer.Serialize(new[] { expectedEmbedding });
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var results = await _provider.GenerateBatchEmbeddingsAsync(chunks);

        // Assert
        results.Should().HaveCount(2);
        results.All(r => r.Success).Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_WhenServiceIsHealthy_ReturnsTrue()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("OK", Encoding.UTF8, "text/plain")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/embeddings")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _provider.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_WhenServiceIsUnhealthy_ReturnsFalse()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("Service Unavailable", Encoding.UTF8, "text/plain")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _provider.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_WhenServiceThrowsException_ReturnsFalse()
    {
        // Arrange
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _provider.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
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
            Content = "Test content for embedding",
            SourceFile = fileItem,
            Position = new ChunkPosition
            {
                StartIndex = 0,
                EndIndex = 25,
                ChunkIndex = 0,
                TotalChunks = 1
            }
        };
    }
}

