using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using RAG.Orchestrator.Api.Models;
using RAG.Orchestrator.Api.Services;
using System.Net;
using System.Text;
using System.Text.Json;

namespace RAG.Tests.Orchestrator;

public class LlmServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<IGlobalSettingsCache> _mockCache;
    private readonly Mock<ILogger<LlmService>> _mockLogger;
    private readonly LlmService _llmService;

    public LlmServiceTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        _mockCache = new Mock<IGlobalSettingsCache>();
        _mockLogger = new Mock<ILogger<LlmService>>();

        _llmService = new LlmService(_httpClient, _mockCache.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [Fact]
    public async Task GenerateResponseAsync_WithValidPrompt_ReturnsResponse()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Url = "http://test-llm.com",
            Model = "test-model",
            IsOllama = true,
            Temperature = 0.7,
            MaxTokens = 100,
            TimeoutMinutes = 5
        };

        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync(settings);

        var responseJson = JsonSerializer.Serialize(new { response = "Test response" });
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _llmService.GenerateResponseAsync("Test prompt");

        // Assert
        result.Should().Be("Test response");
    }

    [Fact]
    public async Task GenerateResponseAsync_WithTgiApi_ReturnsResponse()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Url = "http://test-tgi.com",
            Model = "test-model",
            IsOllama = false,
            Temperature = 0.7,
            MaxTokens = 100,
            TimeoutMinutes = 5
        };

        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync(settings);

        var responseJson = JsonSerializer.Serialize(new { generated_text = "TGI response" });
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _llmService.GenerateResponseAsync("Test prompt");

        // Assert
        result.Should().Be("TGI response");
    }

    [Fact]
    public async Task GenerateResponseWithContextAsync_WithOllama_ReturnsResponseWithContext()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Url = "http://test-ollama.com",
            Model = "test-model",
            IsOllama = true,
            Temperature = 0.7,
            MaxTokens = 100,
            TimeoutMinutes = 5
        };

        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync(settings);

        var context = new int[] { 1, 2, 3, 4, 5 };
        var responseJson = JsonSerializer.Serialize(new
        {
            response = "Response with context",
            context = new int[] { 6, 7, 8, 9, 10 }
        });
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var (response, returnedContext) = await _llmService.GenerateResponseWithContextAsync("Test prompt", context);

        // Assert
        response.Should().Be("Response with context");
        returnedContext.Should().NotBeNull();
        returnedContext.Should().BeEquivalentTo(new int[] { 6, 7, 8, 9, 10 });
    }

    [Fact]
    public async Task GenerateResponseWithContextAsync_WithHttpError_ReturnsErrorMessage()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Url = "http://test-llm.com",
            Model = "test-model",
            IsOllama = true,
            TimeoutMinutes = 5
        };

        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync(settings);
        SetupHttpResponse(HttpStatusCode.InternalServerError, "Server error");

        // Act
        var (response, context) = await _llmService.GenerateResponseWithContextAsync("Test prompt");

        // Assert
        response.Should().Contain("problem generating");
        context.Should().BeNull();
    }

    [Fact]
    public async Task GenerateResponseWithContextAsync_WithTimeout_ReturnsTimeoutMessage()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Url = "http://test-llm.com",
            Model = "test-model",
            IsOllama = true,
            TimeoutMinutes = 5
        };

        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync(settings);
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        // Act
        var (response, context) = await _llmService.GenerateResponseWithContextAsync("Test prompt");

        // Assert
        response.Should().Contain("timed out");
        context.Should().BeNull();
    }

    [Fact]
    public async Task GenerateResponseWithContextAsync_WithNetworkError_ReturnsErrorMessage()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Url = "http://test-llm.com",
            Model = "test-model",
            IsOllama = true,
            TimeoutMinutes = 5
        };

        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync(settings);
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var (response, context) = await _llmService.GenerateResponseWithContextAsync("Test prompt");

        // Assert
        response.Should().Contain("Network error");
        context.Should().BeNull();
    }

    [Fact]
    public async Task GenerateResponseWithContextAsync_WithInvalidJson_ReturnsErrorMessage()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Url = "http://test-llm.com",
            Model = "test-model",
            IsOllama = true,
            TimeoutMinutes = 5
        };

        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync(settings);
        SetupHttpResponse(HttpStatusCode.OK, "Invalid JSON {");

        // Act
        var (response, context) = await _llmService.GenerateResponseWithContextAsync("Test prompt");

        // Assert
        response.Should().Contain("Failed to parse");
        context.Should().BeNull();
    }

    [Fact]
    public async Task GenerateResponseAsync_WithMissingSettings_ReturnsErrorMessage()
    {
        // Arrange
        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync((LlmSettings?)null);

        // Act - GetSettingsAsync throws InvalidOperationException, but it's caught and returns error message
        var result = await _llmService.GenerateResponseAsync("Test prompt");

        // Assert - Exception is caught and returns error message instead of throwing
        result.Should().NotBeNullOrEmpty();
        result.Should().ContainAny("error", "Error", "problem", "Problem");
    }

    [Fact]
    public async Task GenerateResponseWithContextAsync_WithMissingSettings_ReturnsErrorMessage()
    {
        // Arrange
        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync((LlmSettings?)null);

        // Act - GetSettingsAsync throws InvalidOperationException, but it's caught and returns error message
        var (response, context) = await _llmService.GenerateResponseWithContextAsync("Test prompt");

        // Assert - Exception is caught and returns error message instead of throwing
        response.Should().NotBeNullOrEmpty();
        response.Should().ContainAny("error", "Error", "problem", "Problem");
        context.Should().BeNull();
    }

    [Fact]
    public async Task IsHealthyAsync_WithOllama_ChecksTagsEndpoint()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Url = "http://test-ollama.com",
            IsOllama = true,
            TimeoutMinutes = 5
        };

        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync(settings);
        SetupHttpResponse(HttpStatusCode.OK, "{}");

        // Act
        var result = await _llmService.IsHealthyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsHealthyAsync_WithTgi_ChecksHealthEndpoint()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Url = "http://test-tgi.com",
            IsOllama = false,
            TimeoutMinutes = 5
        };

        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync(settings);
        SetupHttpResponse(HttpStatusCode.OK, "{}");

        // Act
        var result = await _llmService.IsHealthyAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsHealthyAsync_WithTimeout_ReturnsFalse()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Url = "http://test-llm.com",
            IsOllama = true,
            TimeoutMinutes = 5
        };

        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync(settings);
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _llmService.IsHealthyAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableModelsAsync_WithOllama_ReturnsModels()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Url = "http://test-ollama.com",
            IsOllama = true,
            TimeoutMinutes = 5
        };

        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync(settings);

        var responseJson = JsonSerializer.Serialize(new
        {
            models = new[]
            {
                new { name = "llama2" },
                new { name = "mistral" }
            }
        });
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _llmService.GetAvailableModelsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("llama2");
        result.Should().Contain("mistral");
    }

    [Fact]
    public async Task GetAvailableModelsAsync_WithTgi_ReturnsSingleModel()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Url = "http://test-tgi.com",
            Model = "tgi-model",
            IsOllama = false,
            TimeoutMinutes = 5
        };

        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync(settings);

        // Act
        var result = await _llmService.GetAvailableModelsAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("tgi-model");
    }

    [Fact]
    public async Task ChatWithHistoryAsync_WithOllama_ReturnsResponse()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Url = "http://test-ollama.com",
            Model = "test-model",
            IsOllama = true,
            Temperature = 0.7,
            MaxTokens = 100,
            ChatEndpoint = "/api/chat",
            TimeoutMinutes = 5
        };

        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync(settings);

        var responseJson = JsonSerializer.Serialize(new
        {
            message = new { content = "Chat response" }
        });
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        var history = new List<LlmChatMessage>
        {
            new LlmChatMessage { Role = "user", Content = "Hello" },
            new LlmChatMessage { Role = "assistant", Content = "Hi there!" }
        };

        // Act
        var result = await _llmService.ChatWithHistoryAsync(history, "How are you?");

        // Assert
        result.Should().Be("Chat response");
    }

    [Fact]
    public async Task ChatWithHistoryAsync_WithTgi_FallsBackToGenerate()
    {
        // Arrange
        var settings = new LlmSettings
        {
            Url = "http://test-tgi.com",
            Model = "test-model",
            IsOllama = false,
            TimeoutMinutes = 5
        };

        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync(settings);

        var responseJson = JsonSerializer.Serialize(new { generated_text = "Fallback response" });
        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        var history = new List<LlmChatMessage>();

        // Act
        var result = await _llmService.ChatWithHistoryAsync(history, "Test message");

        // Assert
        result.Should().Be("Fallback response");
    }

    [Fact]
    public async Task GetSystemMessageAsync_WithLanguage_ReturnsCachedMessage()
    {
        // Arrange
        var settings = new LlmSettings { Url = "http://test.com", TimeoutMinutes = 5 };
        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync(settings);

        // Act - first call
        var result1 = await _llmService.GetSystemMessageAsync("en");

        // Act - second call (should use cache)
        var result2 = await _llmService.GetSystemMessageAsync("en");

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public async Task GetSystemMessageAsync_WithUnsupportedLanguage_FallsBackToEnglish()
    {
        // Arrange
        var settings = new LlmSettings { Url = "http://test.com", TimeoutMinutes = 5 };
        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync(settings);

        // Act
        var result = await _llmService.GetSystemMessageAsync("fr"); // French not supported

        // Assert - Should not throw and return default message
        result.Should().NotBeNullOrEmpty();
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken cancellationToken) =>
            {
                // Set BaseAddress if not set
                if (_httpClient.BaseAddress == null && request.RequestUri != null)
                {
                    var uriBuilder = new UriBuilder(request.RequestUri);
                    uriBuilder.Path = "";
                    uriBuilder.Query = "";
                    _httpClient.BaseAddress = uriBuilder.Uri;
                }

                return new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content, Encoding.UTF8, "application/json")
                };
            });
    }

}

