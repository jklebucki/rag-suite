using Xunit;
using RAG.Orchestrator.Api.Services;
using RAG.Orchestrator.Api.Data;
using Moq;
using Microsoft.EntityFrameworkCore;
using RAG.Orchestrator.Api.Models;
using Microsoft.Extensions.Configuration;

namespace RAG.Tests.Orchestrator;

public class GlobalSettingsServiceTests : IDisposable
{
    private readonly ChatDbContext _context;
    private readonly Mock<IGlobalSettingsCache> _mockCache;
    private readonly GlobalSettingsService _service;

    public GlobalSettingsServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChatDbContext(options);
        _mockCache = new Mock<IGlobalSettingsCache>();
        _service = new GlobalSettingsService(_context, _mockCache.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetLlmSettingsAsync_ReturnsSettingsFromCache()
    {
        // Arrange
        var expectedSettings = new LlmSettings { Url = "http://test.com" };
        _mockCache.Setup(c => c.GetLlmSettingsAsync()).ReturnsAsync(expectedSettings);

        // Act
        var result = await _service.GetLlmSettingsAsync();

        // Assert
        Assert.Equal(expectedSettings, result);
    }

    [Fact]
    public async Task SetLlmSettingsAsync_CallsCacheSet()
    {
        // Arrange
        var settings = new LlmSettings { Url = "http://test.com" };

        // Act
        await _service.SetLlmSettingsAsync(settings);

        // Assert
        _mockCache.Verify(c => c.SetLlmSettingsAsync(settings, _context, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitializeLlmSettingsAsync_WhenNotExists_AddsToDatabase()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s["Url"]).Returns("http://test.com");
        mockSection.Setup(s => s["MaxTokens"]).Returns("3000");
        mockSection.Setup(s => s["Temperature"]).Returns("0.7");
        mockSection.Setup(s => s["Model"]).Returns("test-model");
        mockSection.Setup(s => s["IsOllama"]).Returns("true");
        mockSection.Setup(s => s["TimeoutMinutes"]).Returns("15");
        mockSection.Setup(s => s["ChatEndpoint"]).Returns("/api/chat");
        mockSection.Setup(s => s["GenerateEndpoint"]).Returns("/api/generate");
        mockConfig.Setup(c => c.GetSection("Services:LlmService")).Returns(mockSection.Object);

        // Act
        await _service.InitializeLlmSettingsAsync(mockConfig.Object, _context);

        // Assert
        var setting = await _context.GlobalSettings.FirstOrDefaultAsync(s => s.Key == "LlmService");
        Assert.NotNull(setting);
        Assert.Contains("http://test.com", setting.Value);
    }

    [Fact]
    public async Task InitializeLlmSettingsAsync_WhenExists_DoesNothing()
    {
        // Arrange
        await _context.GlobalSettings.AddAsync(new GlobalSetting { Key = "LlmService", Value = "{}" });
        await _context.SaveChangesAsync();

        var mockConfig = new Mock<IConfiguration>();

        // Act
        await _service.InitializeLlmSettingsAsync(mockConfig.Object, _context);

        // Assert
        var count = await _context.GlobalSettings.CountAsync(s => s.Key == "LlmService");
        Assert.Equal(1, count); // Should not add another
    }
}