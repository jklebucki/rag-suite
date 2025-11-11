using FluentAssertions;
using RAG.Collector.Config;

namespace RAG.Tests.Collector;

public class ConstantsTests
{
    [Fact]
    public void DefaultEmbeddingDimensions_HasCorrectValue()
    {
        // Assert
        Constants.DefaultEmbeddingDimensions.Should().Be(768);
    }

    [Fact]
    public void DefaultMaxTokens_HasCorrectValue()
    {
        // Assert
        Constants.DefaultMaxTokens.Should().Be(512);
    }

    [Fact]
    public void ProgressLogInterval_HasCorrectValue()
    {
        // Assert
        Constants.ProgressLogInterval.Should().Be(50);
    }

    [Fact]
    public void EnumerationYieldInterval_HasCorrectValue()
    {
        // Assert
        Constants.EnumerationYieldInterval.Should().Be(100);
    }

    [Fact]
    public void EmbeddingRequestDelayMs_HasCorrectValue()
    {
        // Assert
        Constants.EmbeddingRequestDelayMs.Should().Be(50);
    }

    [Fact]
    public void HealthCheckTimeoutSeconds_HasCorrectValue()
    {
        // Assert
        Constants.HealthCheckTimeoutSeconds.Should().Be(5);
    }

    [Fact]
    public void ContentHashDisplayLength_HasCorrectValue()
    {
        // Assert
        Constants.ContentHashDisplayLength.Should().Be(12);
    }

    [Fact]
    public void FileMetadataIndexName_HasCorrectValue()
    {
        // Assert
        Constants.FileMetadataIndexName.Should().Be("rag-file-metadata");
    }

    [Fact]
    public void DefaultIndexName_HasCorrectValue()
    {
        // Assert
        Constants.DefaultIndexName.Should().Be("rag-chunks");
    }

    [Fact]
    public void EmbeddingEndpoint_HasCorrectValue()
    {
        // Assert
        Constants.EmbeddingEndpoint.Should().Be("/embed");
    }

    [Fact]
    public void CharactersPerToken_HasCorrectValue()
    {
        // Assert
        Constants.CharactersPerToken.Should().Be(4.0);
    }

    [Fact]
    public void MinParagraphLengthForSentenceSplit_HasCorrectValue()
    {
        // Assert
        Constants.MinParagraphLengthForSentenceSplit.Should().Be(800);
    }

    [Fact]
    public void SentenceGroupingThreshold_HasCorrectValue()
    {
        // Assert
        Constants.SentenceGroupingThreshold.Should().Be(600);
    }

    [Fact]
    public void BreakPointSearchPercentage_HasCorrectValue()
    {
        // Assert
        Constants.BreakPointSearchPercentage.Should().Be(0.8);
    }
}

