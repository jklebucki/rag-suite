using Xunit;
using RAG.Orchestrator.Api.Features.Chat.Prompting;
using RAG.Orchestrator.Api.Localization;
using RAG.Orchestrator.Api.Common.Constants;
using RAG.Abstractions.Search;
using Moq;
using FluentAssertions;

namespace RAG.Tests.Orchestrator;

public class PromptBuilderTests
{
    private readonly Mock<ILanguageService> _mockLanguageService;
    private readonly PromptBuilder _promptBuilder;

    public PromptBuilderTests()
    {
        _mockLanguageService = new Mock<ILanguageService>();
        _promptBuilder = new PromptBuilder(_mockLanguageService.Object);

        // Setup default mock responses
        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        // Default fallback for any unmocked calls
        _mockLanguageService.Setup(ls => ls.GetLocalizedString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string, string>((category, key, lang) => $"{category}.{key}.{lang}");

        // Setup specific mocks for common keys
        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.RagAssistant,
            It.IsAny<string>()))
            .Returns("You are a helpful assistant.");

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.ContextInstruction,
            It.IsAny<string>()))
            .Returns("Use the following context to answer.");

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            LocalizationKeys.SystemPrompts.KnowledgeBaseContext,
            LocalizationKeys.SystemPrompts.KnowledgeBaseContext,
            It.IsAny<string>()))
            .Returns("Knowledge base context:");

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "ui_labels",
            LocalizationKeys.UiLabels.User,
            It.IsAny<string>()))
            .Returns("User:");

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "ui_labels",
            LocalizationKeys.UiLabels.Assistant,
            It.IsAny<string>()))
            .Returns("Assistant:");

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.CurrentQuestion,
            It.IsAny<string>()))
            .Returns("Current question:");

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.Response,
            It.IsAny<string>()))
            .Returns("Response:");
    }

    [Fact]
    public void BuildContextualPrompt_WithSearchResults_IncludesDocuments()
    {
        // Arrange
        var searchResults = new[]
        {
            new SearchResult("id1", "title1", "content1", 1.0, "source1", "pdf", null, "file1.pdf", new Dictionary<string, object>(), DateTime.UtcNow, DateTime.UtcNow)
        };

        var context = new PromptContext
        {
            UserMessage = "Hello",
            SearchResults = searchResults,
            ResponseLanguage = "en",
            UseDocumentSearch = true
        };

        // Act
        var result = _promptBuilder.BuildContextualPrompt(context);

        // Assert
        result.Should().Contain("file1.pdf");
        result.Should().Contain("content1");
        result.Should().Contain("Hello");
    }

    [Fact]
    public void BuildContextualPrompt_WithoutSearchResults_ExcludesDocuments()
    {
        // Arrange
        var context = new PromptContext
        {
            UserMessage = "Hello",
            SearchResults = Array.Empty<SearchResult>(),
            ResponseLanguage = "en",
            UseDocumentSearch = false
        };

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.RagAssistantNoDocs,
            It.IsAny<string>()))
            .Returns("You are a helpful assistant without documents.");

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.ContextInstructionNoDocs,
            It.IsAny<string>()))
            .Returns("Answer without documents.");

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            LocalizationKeys.SystemPrompts.NoDocumentSearchNote,
            LocalizationKeys.SystemPrompts.NoDocumentSearchNote,
            It.IsAny<string>()))
            .Returns("Note: Document search is disabled.");

        // Act
        var result = _promptBuilder.BuildContextualPrompt(context);

        // Assert
        result.Should().Contain("You are a helpful assistant without documents.");
        result.Should().Contain("Note: Document search is disabled.");
        result.Should().NotContain("Knowledge base context:");
    }

    [Fact]
    public void BuildContextualPrompt_WithConversationHistory_IncludesHistory()
    {
        // Arrange
        var conversationHistory = new List<MessageContext>
        {
            new MessageContext { Role = "user", Content = "Previous question" },
            new MessageContext { Role = "assistant", Content = "Previous answer" },
            new MessageContext { Role = "user", Content = "New question" }
        };

        var context = new PromptContext
        {
            UserMessage = "New question",
            SearchResults = Array.Empty<SearchResult>(),
            ConversationHistory = conversationHistory,
            ResponseLanguage = "en",
            UseDocumentSearch = false
        };

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.RagAssistantNoDocs,
            It.IsAny<string>()))
            .Returns("You are a helpful assistant without documents.");

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.ContextInstructionNoDocs,
            It.IsAny<string>()))
            .Returns("Answer without documents.");

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.ConversationHistory,
            It.IsAny<string>()))
            .Returns("Conversation history:");

        // Act
        var result = _promptBuilder.BuildContextualPrompt(context);

        // Assert
        result.Should().Contain("Previous question");
        result.Should().Contain("Previous answer");
        result.Should().Contain("New question");
    }

    [Fact]
    public void BuildContextualPrompt_WithMultipleSearchResults_IncludesSourcesSummary()
    {
        // Arrange
        var searchResults = new[]
        {
            new SearchResult("id1", "title1", "content1", 1.0, "source1", "pdf", null, "file1.pdf", new Dictionary<string, object>(), DateTime.UtcNow, DateTime.UtcNow),
            new SearchResult("id2", "title2", "content2", 0.9, "source2", "pdf", null, "file2.pdf", new Dictionary<string, object>(), DateTime.UtcNow, DateTime.UtcNow)
        };

        var context = new PromptContext
        {
            UserMessage = "Hello",
            SearchResults = searchResults,
            ResponseLanguage = "en",
            UseDocumentSearch = true
        };

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "ui_labels",
            LocalizationKeys.UiLabels.Sources,
            It.IsAny<string>()))
            .Returns("Sources");

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "ui_labels",
            LocalizationKeys.UiLabels.Used,
            It.IsAny<string>()))
            .Returns("used");

        // Act
        var result = _promptBuilder.BuildContextualPrompt(context);

        // Assert
        result.Should().Contain("file1.pdf");
        result.Should().Contain("file2.pdf");
        result.Should().Contain("Sources used:");
    }

    [Fact]
    public void BuildMultilingualContextualPrompt_IncludesLanguageInstructions()
    {
        // Arrange
        var context = new PromptContext
        {
            UserMessage = "Hello",
            SearchResults = Array.Empty<SearchResult>(),
            ResponseLanguage = "pl",
            DetectedLanguage = "en",
            UseDocumentSearch = false
        };

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "instructions",
            LocalizationKeys.Instructions.RespondInLanguage,
            It.IsAny<string>()))
            .Returns("Respond in the specified language.");

        // Act
        var result = _promptBuilder.BuildMultilingualContextualPrompt(context);

        // Assert
        result.Should().Contain("IMPORTANT: Respond in the specified language.");
        result.Should().Contain("MUST RESPOND IN: PL");
        result.Should().Contain("CRITICAL: Respond in the specified language.");
    }

    [Fact]
    public void BuildMultilingualContextualPrompt_WithDetectedLanguage_ShowsDetectedLanguage()
    {
        // Arrange
        var context = new PromptContext
        {
            UserMessage = "Hello",
            SearchResults = Array.Empty<SearchResult>(),
            ResponseLanguage = "pl",
            DetectedLanguage = "en",
            UseDocumentSearch = false
        };

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "instructions",
            LocalizationKeys.Instructions.RespondInLanguage,
            It.IsAny<string>()))
            .Returns("Respond in the specified language.");

        // Act
        var result = _promptBuilder.BuildMultilingualContextualPrompt(context);

        // Assert
        result.Should().Contain("Hello");
        result.Should().Contain("en");
    }

    [Fact]
    public void BuildDocumentsContext_WithSearchResults_FormatsDocuments()
    {
        // Arrange
        var searchResults = new[]
        {
            new SearchResult("id1", "title1", "content1", 1.0, "source1", "pdf", null, "file1.pdf", new Dictionary<string, object>(), DateTime.UtcNow, DateTime.UtcNow)
        };

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "ui_labels",
            LocalizationKeys.UiLabels.Source,
            It.IsAny<string>()))
            .Returns("Source");

        // Act
        var result = _promptBuilder.BuildDocumentsContext(searchResults, "en");

        // Assert
        result.Should().Contain("file1.pdf");
        result.Should().Contain("content1");
        result.Should().Contain("MUST RESPOND IN: EN");
    }

    [Fact]
    public void BuildDocumentsContext_WithEmptyResults_ReturnsEmptyString()
    {
        // Arrange
        var searchResults = Array.Empty<SearchResult>();

        // Act
        var result = _promptBuilder.BuildDocumentsContext(searchResults, "en");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildDocumentsContext_WithMultipleResults_IncludesSourcesSummary()
    {
        // Arrange
        var searchResults = new[]
        {
            new SearchResult("id1", "title1", "content1", 1.0, "source1", "pdf", null, "file1.pdf", new Dictionary<string, object>(), DateTime.UtcNow, DateTime.UtcNow),
            new SearchResult("id2", "title2", "content2", 0.9, "source2", "pdf", null, "file2.pdf", new Dictionary<string, object>(), DateTime.UtcNow, DateTime.UtcNow)
        };

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "ui_labels",
            LocalizationKeys.UiLabels.Sources,
            It.IsAny<string>()))
            .Returns("Sources");

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "ui_labels",
            LocalizationKeys.UiLabels.Used,
            It.IsAny<string>()))
            .Returns("used");

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "instructions",
            LocalizationKeys.Instructions.BeHonest,
            It.IsAny<string>()))
            .Returns("Be honest if you don't know.");

        // Act
        var result = _promptBuilder.BuildDocumentsContext(searchResults, "en");

        // Assert
        result.Should().Contain("Sources used:");
        result.Should().Contain("REMINDER: Be honest if you don't know.");
    }

    [Fact]
    public void BuildContextualPrompt_WithLongConversationHistory_IncludesOnlyLast5Messages()
    {
        // Arrange
        var conversationHistory = new List<MessageContext>();
        for (int i = 0; i < 10; i++)
        {
            conversationHistory.Add(new MessageContext { Role = "user", Content = $"Question {i}" });
            conversationHistory.Add(new MessageContext { Role = "assistant", Content = $"Answer {i}" });
        }

        var context = new PromptContext
        {
            UserMessage = "New question",
            SearchResults = Array.Empty<SearchResult>(),
            ConversationHistory = conversationHistory,
            ResponseLanguage = "en",
            UseDocumentSearch = false
        };

        _mockLanguageService.Setup(ls => ls.GetLocalizedString(
            "system_prompts",
            LocalizationKeys.SystemPrompts.ConversationHistory,
            It.IsAny<string>()))
            .Returns("Conversation history:");

        // Act
        var result = _promptBuilder.BuildContextualPrompt(context);

        // Assert
        // Should include recent messages (last 5 excluding current)
        result.Should().Contain("Question");
        result.Should().Contain("Answer");
        // Should not include very old messages
        result.Should().NotContain("Question 0");
        result.Should().NotContain("Answer 0");
    }

    [Fact]
    public void BuildContextualPrompt_WithFileName_UsesFileNameOverSource()
    {
        // Arrange
        var searchResults = new[]
        {
            new SearchResult("id1", "title1", "content1", 1.0, "source1", "pdf", null, "file1.pdf", new Dictionary<string, object>(), DateTime.UtcNow, DateTime.UtcNow)
        };

        var context = new PromptContext
        {
            UserMessage = "Hello",
            SearchResults = searchResults,
            ResponseLanguage = "en",
            UseDocumentSearch = true
        };

        // Act
        var result = _promptBuilder.BuildContextualPrompt(context);

        // Assert
        result.Should().Contain("[file1.pdf]");
        result.Should().NotContain("[source1]");
    }

    [Fact]
    public void BuildContextualPrompt_WithoutFileName_UsesSource()
    {
        // Arrange
        var searchResults = new[]
        {
            new SearchResult("id1", "title1", "content1", 1.0, "source1", "pdf", null, null, new Dictionary<string, object>(), DateTime.UtcNow, DateTime.UtcNow)
        };

        var context = new PromptContext
        {
            UserMessage = "Hello",
            SearchResults = searchResults,
            ResponseLanguage = "en",
            UseDocumentSearch = true
        };

        // Act
        var result = _promptBuilder.BuildContextualPrompt(context);

        // Assert
        result.Should().Contain("[source1]");
    }
}

