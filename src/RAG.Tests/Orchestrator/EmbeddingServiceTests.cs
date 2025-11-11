using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using RAG.Orchestrator.Api.Features.Embeddings;
using System.Net;
using System.Text;
using System.Text.Json;

namespace RAG.Tests.Orchestrator;

public class EmbeddingServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<EmbeddingService>> _mockLogger;
    private readonly EmbeddingService _embeddingService;

    public EmbeddingServiceTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<EmbeddingService>>();

        _mockConfiguration.Setup(c => c["Services:EmbeddingService:Url"])
            .Returns("http://test-embedding-service:8580");

        _embeddingService = new EmbeddingService(
            _httpClient,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithValidText_ReturnsEmbedding()
    {
        // Arrange
        var text = "Test text for embedding";
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
        var result = await _embeddingService.GenerateEmbeddingAsync(text);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(768);
        result.Should().BeEquivalentTo(expectedEmbedding);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithEmptyResponse_ThrowsException()
    {
        // Arrange
        var text = "Test text";
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

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _embeddingService.GenerateEmbeddingAsync(text));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithHttpError_ThrowsException()
    {
        // Arrange
        var text = "Test text";
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

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _embeddingService.GenerateEmbeddingAsync(text));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithInvalidJson_ThrowsException()
    {
        // Arrange
        var text = "Test text";
        var responseJson = "invalid json";
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

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _embeddingService.GenerateEmbeddingAsync(text));
    }

    [Fact]
    public void EmbeddingDimension_ReturnsCorrectValue()
    {
        // Act
        var dimension = _embeddingService.EmbeddingDimension;

        // Assert
        dimension.Should().Be(768);
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
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains("/health")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _embeddingService.IsAvailableAsync();

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
        var result = await _embeddingService.IsAvailableAsync();

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
        var result = await _embeddingService.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WithMultipleEmbeddings_ReturnsFirst()
    {
        // Arrange
        var text = "Test text";
        var embedding1 = new float[768];
        var embedding2 = new float[768];
        for (int i = 0; i < 768; i++)
        {
            embedding1[i] = 1.0f;
            embedding2[i] = 2.0f;
        }

        var responseJson = JsonSerializer.Serialize(new[] { embedding1, embedding2 });
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
        var result = await _embeddingService.GenerateEmbeddingAsync(text);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(768);
        result.Should().BeEquivalentTo(embedding1);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_LogsDebugInformation()
    {
        // Arrange
        var text = "Test text";
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
        await _embeddingService.GenerateEmbeddingAsync(text);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Generating embedding")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

