using Xunit;
using RAG.Orchestrator.Api.Features.Chat;
using RAG.Orchestrator.Api.Models;
using System.Collections.Generic;
using Moq;
using RAG.Orchestrator.Api.Localization;
using RAG.Abstractions.Search;

namespace RAG.Tests;

public class ChatHelperTests
{
    [Fact]
    public void ConvertToLlmChatMessages_WithChatMessages_ReturnsLlmChatMessages()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage { Role = "user", Content = "Hello" },
            new ChatMessage { Role = "assistant", Content = "Hi there" },
            new ChatMessage { Role = "system", Content = "System message" } // Should be excluded
        };

        // Act
        var result = ChatHelper.ConvertToLlmChatMessages(messages);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("user", result[0].Role);
        Assert.Equal("Hello", result[0].Content);
        Assert.Equal("assistant", result[1].Role);
        Assert.Equal("Hi there", result[1].Content);
    }

    [Fact]
    public void ConvertToLlmChatMessages_WithUserChatMessages_ReturnsLlmChatMessages()
    {
        // Arrange
        var messages = new List<UserChatMessage>
        {
            new UserChatMessage("1", "user", "Hello", DateTime.UtcNow, null, null, null),
            new UserChatMessage("2", "assistant", "Hi there", DateTime.UtcNow, null, null, null),
            new UserChatMessage("3", "system", "System message", DateTime.UtcNow, null, null, null) // Should be excluded
        };

        // Act
        var result = ChatHelper.ConvertToLlmChatMessages(messages);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("user", result[0].Role);
        Assert.Equal("Hello", result[0].Content);
        Assert.Equal("assistant", result[1].Role);
        Assert.Equal("Hi there", result[1].Content);
    }

    [Fact]
    public void ConvertToLlmChatMessages_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var messages = new List<ChatMessage>();

        // Act
        var result = ChatHelper.ConvertToLlmChatMessages(messages);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ConvertToLlmChatMessages_WithOnlySystemMessages_ReturnsEmptyList()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage { Role = "system", Content = "System message" }
        };

        // Act
        var result = ChatHelper.ConvertToLlmChatMessages(messages);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void BuildContextualPrompt_WithSearchResults_IncludesDocuments()
    {
        // Arrange
        var mockLanguageService = new Mock<ILanguageService>();
        mockLanguageService.Setup(ls => ls.GetLocalizedString("system_prompts", "rag_assistant", "en"))
            .Returns("You are a helpful assistant.");
        mockLanguageService.Setup(ls => ls.GetLocalizedString("system_prompts", "context_instruction", "en"))
            .Returns("Use the following context to answer.");
        mockLanguageService.Setup(ls => ls.GetLocalizedString("system_prompts", "knowledge_base_context", "en"))
            .Returns("Knowledge base context:");
        mockLanguageService.Setup(ls => ls.GetLocalizedString("system_prompts", "sources_summary", "en"))
            .Returns("Sources:");

        var searchResults = new[]
        {
            new SearchResult("content1", "source1", "", 1.0, "pdf", "title1", null, null, new Dictionary<string, object>(), DateTime.UtcNow, DateTime.UtcNow)
        };
        var conversationHistory = new List<ChatMessage>();
        var userMessage = "Hello";

        // Act
        var result = ChatHelper.BuildContextualPrompt(userMessage, searchResults, conversationHistory, mockLanguageService.Object, "en", true);

        // Assert
        Assert.Contains("Knowledge base context:", result);
        Assert.Contains("[source1]", result);
        Assert.Contains("content1", result);
    }

    [Fact]
    public void BuildContextualPrompt_WithoutSearchResults_ExcludesDocuments()
    {
        // Arrange
        var mockLanguageService = new Mock<ILanguageService>();
        mockLanguageService.Setup(ls => ls.GetLocalizedString("system_prompts", "rag_assistant_no_docs", "en"))
            .Returns("You are a helpful assistant without documents.");
        mockLanguageService.Setup(ls => ls.GetLocalizedString("system_prompts", "context_instruction_no_docs", "en"))
            .Returns("Answer without documents.");
        mockLanguageService.Setup(ls => ls.GetLocalizedString("system_prompts", "no_document_search_note", "en"))
            .Returns("No documents available.");

        var searchResults = Array.Empty<SearchResult>();
        var conversationHistory = new List<ChatMessage>();
        var userMessage = "Hello";

        // Act
        var result = ChatHelper.BuildContextualPrompt(userMessage, searchResults, conversationHistory, mockLanguageService.Object, "en", false);

        // Assert
        Assert.Contains("No documents available.", result);
        Assert.Contains("You are a helpful assistant without documents.", result);
    }
}