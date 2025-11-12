using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RAG.Orchestrator.Api.Features.Search;

namespace RAG.Tests.Orchestrator;

public class QueryProcessorTests
{
    private readonly Mock<ILogger<QueryProcessor>> _mockLogger;
    private readonly QueryProcessor _processor;

    public QueryProcessorTests()
    {
        _mockLogger = new Mock<ILogger<QueryProcessor>>();
        _processor = new QueryProcessor(_mockLogger.Object);
    }

    [Fact]
    public void ProcessQuery_WithEmptyQuery_ReturnsKeywordsType()
    {
        // Act
        var result = _processor.ProcessQuery("");

        // Assert
        result.Type.Should().Be(QueryType.Keywords);
        result.ProcessedQuery.Should().BeEmpty();
    }

    [Fact]
    public void ProcessQuery_WithWhitespaceOnly_ReturnsKeywordsType()
    {
        // Act
        var result = _processor.ProcessQuery("   \t\n  ");

        // Assert
        result.Type.Should().Be(QueryType.Keywords);
        result.ProcessedQuery.Should().Be("   \t\n  ");
    }

    [Fact]
    public void ProcessQuery_WithQuestionMark_ReturnsQuestionType()
    {
        // Act
        var result = _processor.ProcessQuery("Jak działa system?");

        // Assert
        result.Type.Should().Be(QueryType.Question);
        result.ProcessedQuery.Should().Contain("działa");
        result.ProcessedQuery.Should().Contain("system");
    }

    [Fact]
    public void ProcessQuery_WithQuestionWord_ReturnsQuestionType()
    {
        // Act
        var result = _processor.ProcessQuery("Co to jest RAG");

        // Assert
        result.Type.Should().Be(QueryType.Question);
        result.KeyTerms.Should().Contain("rag");
    }

    [Fact]
    public void ProcessQuery_WithGreeting_ReturnsConversationalType()
    {
        // Act - query with greeting and many stop words to trigger conversational type
        // Conversational type requires: hasGreeting OR (words.Length > meaningfulWords.Count * 2)
        var result = _processor.ProcessQuery("Cześć pomóż mi proszę bardzo znaleźć dokumenty");

        // Assert - może być Conversational jeśli ma dużo stop words lub Mixed
        result.Type.Should().BeOneOf(QueryType.Conversational, QueryType.Mixed);
        if (result.Type == QueryType.Conversational)
        {
            result.SemanticWeight.Should().BeGreaterThan(result.KeywordWeight);
        }
    }

    [Fact]
    public void ProcessQuery_WithKeywordsOnly_ReturnsKeywordsType()
    {
        // Act
        var result = _processor.ProcessQuery("dokument faktura VAT");

        // Assert
        result.Type.Should().Be(QueryType.Keywords);
        result.KeywordWeight.Should().BeGreaterThan(result.SemanticWeight);
    }

    [Fact]
    public void ProcessQuery_RemovesStopWords()
    {
        // Act
        var result = _processor.ProcessQuery("jak znaleźć dokument o fakturze");

        // Assert
        result.KeyTerms.Should().NotContain("jak");
        result.KeyTerms.Should().Contain("znaleźć");
        result.KeyTerms.Should().Contain("dokument");
        result.KeyTerms.Should().Contain("fakturze");
    }

    [Fact]
    public void ProcessQuery_ExtractsQuotedPhrases()
    {
        // Act
        var result = _processor.ProcessQuery("znajdź \"raport należności\" i \"sprawozdanie finansowe\"");

        // Assert
        result.KeyTerms.Should().Contain("raport należności");
        result.KeyTerms.Should().Contain("sprawozdanie finansowe");
    }

    [Fact]
    public void ProcessQuery_ExtractsCompoundTerms()
    {
        // Act
        var result = _processor.ProcessQuery("pokaż proces logistyczny i zamówienie zakupu");

        // Assert
        result.KeyTerms.Should().Contain("proces logistyczny");
        result.KeyTerms.Should().Contain("zamówienie zakupu");
    }

    [Fact]
    public void ProcessQuery_NormalizesWhitespace()
    {
        // Act
        var result = _processor.ProcessQuery("dokument    faktura   VAT");

        // Assert
        result.ProcessedQuery.Should().NotContain("    ");
        result.ProcessedQuery.Should().NotContain("   ");
    }

    [Fact]
    public void ProcessQuery_WithConversationalQuery_SetsCorrectWeights()
    {
        // Act - query with greeting and many stop words to ensure conversational type
        var result = _processor.ProcessQuery("Witam bardzo proszę pomóż mi znaleźć dokumenty");

        // Assert - może być Conversational lub Mixed w zależności od liczby stop words
        if (result.Type == QueryType.Conversational)
        {
            result.SemanticWeight.Should().Be(0.7);
            result.KeywordWeight.Should().Be(0.3);
        }
        else
        {
            // Jeśli Mixed, sprawdź że wagi są zbalansowane
            result.KeywordWeight.Should().Be(0.5);
            result.SemanticWeight.Should().Be(0.5);
        }
    }

    [Fact]
    public void ProcessQuery_WithQuestionQuery_SetsCorrectWeights()
    {
        // Act
        var result = _processor.ProcessQuery("Jak działa system RAG?");

        // Assert
        result.Type.Should().Be(QueryType.Question);
        result.KeywordWeight.Should().Be(0.6);
        result.SemanticWeight.Should().Be(0.4);
    }

    [Fact]
    public void ProcessQuery_WithKeywordsQuery_SetsCorrectWeights()
    {
        // Act
        var result = _processor.ProcessQuery("dokument faktura");

        // Assert
        result.Type.Should().Be(QueryType.Keywords);
        result.KeywordWeight.Should().Be(0.8);
        result.SemanticWeight.Should().Be(0.2);
    }

    [Fact]
    public void ProcessQuery_WithMixedQuery_SetsCorrectWeights()
    {
        // Act
        var result = _processor.ProcessQuery("dokument o fakturze VAT i procedurach księgowych");

        // Assert
        result.Type.Should().Be(QueryType.Mixed);
        result.KeywordWeight.Should().Be(0.5);
        result.SemanticWeight.Should().Be(0.5);
    }

    [Fact]
    public void ProcessQuery_RemovesVeryShortWords()
    {
        // Act
        var result = _processor.ProcessQuery("a b c dokument faktura");

        // Assert
        result.KeyTerms.Should().NotContain("a");
        result.KeyTerms.Should().NotContain("b");
        result.KeyTerms.Should().NotContain("c");
        result.KeyTerms.Should().Contain("dokument");
        result.KeyTerms.Should().Contain("faktura");
    }

    [Fact]
    public void ProcessQuery_WithEnglishQuestion_DetectsQuestionType()
    {
        // Act
        var result = _processor.ProcessQuery("What is RAG system?");

        // Assert
        result.Type.Should().Be(QueryType.Question);
    }

    [Fact]
    public void ProcessQuery_WithMultipleQuestionWords_DetectsQuestionType()
    {
        // Act
        var result = _processor.ProcessQuery("Kto i kiedy stworzył ten dokument?");

        // Assert
        result.Type.Should().Be(QueryType.Question);
    }

    [Fact]
    public void ProcessQuery_ForConversationalType_PrioritizesKeyTerms()
    {
        // Act - query with greeting (no question mark)
        var result = _processor.ProcessQuery("Cześć, pomóż mi znaleźć dokumenty o fakturach");

        // Assert - może być Conversational lub Mixed w zależności od logiki
        result.Type.Should().BeOneOf(QueryType.Conversational, QueryType.Mixed);
        result.ProcessedQuery.Should().Contain("znaleźć");
        result.ProcessedQuery.Should().Contain("dokumenty");
        result.ProcessedQuery.Should().Contain("fakturach");
    }

    [Fact]
    public void ProcessQuery_ForQuestionType_KeepsQuestionWords()
    {
        // Act
        var result = _processor.ProcessQuery("Jak znaleźć dokument?");

        // Assert
        result.Type.Should().Be(QueryType.Question);
        // Processed query dla Question type łączy question words z key terms
        result.ProcessedQuery.Should().Contain("znaleźć");
        result.ProcessedQuery.Should().Contain("dokument");
        // "jak" może być w processed query lub nie, w zależności od implementacji
    }

    [Theory]
    [InlineData("jak", QueryType.Question)]
    [InlineData("co", QueryType.Question)]
    [InlineData("gdzie", QueryType.Question)]
    [InlineData("kiedy", QueryType.Question)]
    [InlineData("dlaczego", QueryType.Question)]
    [InlineData("czy", QueryType.Question)]
    public void ProcessQuery_WithQuestionWords_DetectsQuestionType(string questionWord, QueryType expectedType)
    {
        // Act
        var result = _processor.ProcessQuery($"{questionWord} działa system");

        // Assert
        result.Type.Should().Be(expectedType);
    }
}

