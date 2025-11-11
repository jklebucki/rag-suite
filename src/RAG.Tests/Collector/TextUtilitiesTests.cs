using FluentAssertions;
using RAG.Collector.ContentExtractors;

namespace RAG.Tests.Collector;

public class TextUtilitiesTests
{
    [Fact]
    public void EstimateWordCount_WithEmptyString_ReturnsZero()
    {
        // Act
        var result = TextUtilities.EstimateWordCount("");

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void EstimateWordCount_WithWhitespaceOnly_ReturnsZero()
    {
        // Act
        var result = TextUtilities.EstimateWordCount("   \t\n\r  ");

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void EstimateWordCount_WithSingleWord_ReturnsOne()
    {
        // Act
        var result = TextUtilities.EstimateWordCount("word");

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void EstimateWordCount_WithMultipleWords_ReturnsCorrectCount()
    {
        // Act
        var result = TextUtilities.EstimateWordCount("This is a test sentence with multiple words");

        // Assert
        result.Should().Be(8);
    }

    [Fact]
    public void EstimateWordCount_WithNewlines_CountsWordsCorrectly()
    {
        // Act
        var result = TextUtilities.EstimateWordCount("Line one\nLine two\nLine three");

        // Assert
        result.Should().Be(6);
    }

    [Fact]
    public void EstimateWordCount_WithTabs_CountsWordsCorrectly()
    {
        // Act
        var result = TextUtilities.EstimateWordCount("Word1\tWord2\tWord3");

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public void EstimateWordCount_WithMultipleSpaces_CountsWordsCorrectly()
    {
        // Act
        var result = TextUtilities.EstimateWordCount("Word1    Word2     Word3");

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public void EstimateWordCount_WithNull_ReturnsZero()
    {
        // Act
        var result = TextUtilities.EstimateWordCount(null!);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void EstimateWordCount_WithComplexText_HandlesCorrectly()
    {
        // Arrange
        var text = "This is a complex sentence.\nIt has multiple lines.\nAnd various\tseparators.";

        // Act
        var result = TextUtilities.EstimateWordCount(text);

        // Assert
        // "This is a complex sentence." = 5 words
        // "It has multiple lines." = 4 words  
        // "And various\tseparators." = 3 words
        // Total = 12 words
        result.Should().Be(12);
    }
}

