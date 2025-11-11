using Microsoft.Extensions.Logging;
using Moq;
using RAG.Orchestrator.Api.Localization;

namespace RAG.Tests.Orchestrator;

public class LanguageServiceTests
{
    private readonly Mock<ILocalizedResources> _mockLocalizedResources;
    private readonly Mock<ILogger<LanguageService>> _mockLogger;
    private readonly LanguageConfiguration _configuration;
    private readonly LanguageService _languageService;

    public LanguageServiceTests()
    {
        _mockLocalizedResources = new Mock<ILocalizedResources>();
        _mockLogger = new Mock<ILogger<LanguageService>>();
        _configuration = new LanguageConfiguration
        {
            DefaultLanguage = "en",
            SupportedLanguages = new List<string> { "pl", "en", "hu", "nl", "ro" }
        };
        _languageService = new LanguageService(_mockLocalizedResources.Object, _configuration, _mockLogger.Object);
    }

    [Fact]
    public void DetectLanguage_WithPolishText_ReturnsPolish()
    {
        // Arrange
        var text = "Czy możesz mi pomóc?";

        // Act
        var result = _languageService.DetectLanguage(text);

        // Assert
        Assert.Equal("pl", result);
    }

    [Fact]
    public void DetectLanguage_WithEnglishText_ReturnsEnglish()
    {
        // Arrange
        var text = "What can I help you with?";

        // Act
        var result = _languageService.DetectLanguage(text);

        // Assert
        Assert.Equal("en", result);
    }

    [Fact]
    public void DetectLanguage_WithEmptyText_ReturnsDefaultLanguage()
    {
        // Arrange
        var text = "";

        // Act
        var result = _languageService.DetectLanguage(text);

        // Assert
        Assert.Equal("en", result);
    }

    [Fact]
    public void DetectLanguage_WithUnsupportedLanguage_ReturnsDefaultLanguage()
    {
        // Arrange
        var text = "Bonjour, comment allez-vous?"; // French, not supported

        // Act
        var result = _languageService.DetectLanguage(text);

        // Assert
        Assert.Equal("en", result);
    }

    [Fact]
    public void GetDefaultLanguage_ReturnsConfiguredDefault()
    {
        // Act
        var result = _languageService.GetDefaultLanguage();

        // Assert
        Assert.Equal("en", result);
    }

    [Fact]
    public void NormalizeLanguage_WithSupportedLanguage_ReturnsSame()
    {
        // Act
        var result = _languageService.NormalizeLanguage("pl");

        // Assert
        Assert.Equal("pl", result);
    }

    [Fact]
    public void NormalizeLanguage_WithUnsupportedLanguage_ReturnsDefault()
    {
        // Act
        var result = _languageService.NormalizeLanguage("fr");

        // Assert
        Assert.Equal("en", result);
    }
}