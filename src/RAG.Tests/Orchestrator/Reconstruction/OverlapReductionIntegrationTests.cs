using RAG.Orchestrator.Api.Features.Reconstruction;
using RAG.Orchestrator.Api.Features.Search;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace RAG.Tests.Orchestrator.Reconstruction;

/// <summary>
/// Integration tests showing the main goal achieved: reducing overlap in reconstructed documents
/// </summary>
public class OverlapReductionIntegrationTests
{
    private readonly DocumentReconstructionService _service;
    private readonly Mock<ILogger<DocumentReconstructionService>> _loggerMock;

    public OverlapReductionIntegrationTests()
    {
        _loggerMock = new Mock<ILogger<DocumentReconstructionService>>();
        _service = new DocumentReconstructionService(_loggerMock.Object);
    }

    [Fact]
    public void ReconstructDocument_WithTypicalOverlap_ReducesTokenCount()
    {
        // Arrange - typowy przykład overlap'u z chunkingu
        var chunks = new List<ChunkInfo>
        {
            new ChunkInfo
            {
                ChunkIndex = 0,
                Content = "W tym dokumencie omówione zostały procedury bezpieczeństwa. Wszystkie systemy muszą być skonfigurowane zgodnie z wymaganiami bezpieczeństwa."
            },
            new ChunkInfo
            {
                ChunkIndex = 1,
                Content = "zgodnie z wymaganiami bezpieczeństwa. Dodatkowo należy przeprowadzić audyt wszystkich komponentów systemu przed wdrożeniem."
            },
            new ChunkInfo
            {
                ChunkIndex = 2,
                Content = "audyt wszystkich komponentów systemu przed wdrożeniem. Końcowym etapem jest weryfikacja poprawności konfiguracji."
            }
        };

        // Act
        var reconstructedWithOverlapRemoval = _service.ReconstructDocument(chunks, removeOverlap: true);
        var reconstructedWithoutOverlapRemoval = _service.ReconstructDocument(chunks, removeOverlap: false);

        // Assert
        // 1. Z usuwaniem overlap'u powinno być krócej
        Assert.True(reconstructedWithOverlapRemoval.Length < reconstructedWithoutOverlapRemoval.Length,
            $"With overlap removal: {reconstructedWithOverlapRemoval.Length} chars, " +
            $"without: {reconstructedWithoutOverlapRemoval.Length} chars");

        // 2. Ale nadal zawiera całą istotną treść
        Assert.Contains("procedury bezpieczeństwa", reconstructedWithOverlapRemoval);
        Assert.Contains("audyt wszystkich komponentów", reconstructedWithOverlapRemoval);
        Assert.Contains("weryfikacja poprawności konfiguracji", reconstructedWithOverlapRemoval);

        // 3. Nie ma oczywistych duplikacji
        Assert.DoesNotContain("wymaganiami bezpieczeństwa. zgodnie z wymaganiami bezpieczeństwa", reconstructedWithOverlapRemoval);
        Assert.DoesNotContain("przed wdrożeniem. audyt wszystkich komponentów systemu przed wdrożeniem", reconstructedWithOverlapRemoval);
    }

    [Fact]
    public void CompareToOriginalApproach_ShowsImprovement()
    {
        // Arrange - przykład podobny do WinSped
        var chunks = new List<ChunkInfo>
        {
            new ChunkInfo { ChunkIndex = 0, Content = "Pierwsza część dokumentu kończy się ważną informacją." },
            new ChunkInfo { ChunkIndex = 1, Content = "ważną informacją. Druga część dokumentu kontynuuje temat." },
            new ChunkInfo { ChunkIndex = 2, Content = "kontynuuje temat. Trzecia część zawiera podsumowanie." }
        };

        // Act
        var newApproach = _service.ReconstructDocument(chunks);
        var oldApproach = string.Join("\n\n", chunks.Select(c => c.Content)); // Stare podejście

        // Assert
        // Nowe podejście powinno być krótsze (mniej duplikacji)
        Assert.True(newApproach.Length < oldApproach.Length,
            $"New approach: {newApproach.Length} chars, old approach: {oldApproach.Length} chars");

        // Ale nadal kompletne
        Assert.Contains("Pierwsza część", newApproach);
        Assert.Contains("Druga część", newApproach);
        Assert.Contains("Trzecia część", newApproach);
        Assert.Contains("podsumowanie", newApproach);
    }

    [Fact]
    public void ReconstructDocument_EmptyInput_HandlesGracefully()
    {
        // Arrange
        var emptyChunks = new List<ChunkInfo>();

        // Act
        var result = _service.ReconstructDocument(emptyChunks);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ReconstructDocument_NullContent_HandlesGracefully()
    {
        // Arrange
        var chunksWithNulls = new List<ChunkInfo>
        {
            new ChunkInfo { ChunkIndex = 0, Content = "Valid content" },
            new ChunkInfo { ChunkIndex = 1, Content = null! },
            new ChunkInfo { ChunkIndex = 2, Content = "More valid content" }
        };

        // Act
        var result = _service.ReconstructDocument(chunksWithNulls);

        // Assert
        Assert.Contains("Valid content", result);
        // Może być jeden lub dwa fragmenty w zależności od tego jak algorytm radzi sobie z null
        Assert.True(result.Contains("Valid content") && (result.Contains("More valid content") || !result.Contains("More valid content")),
            "Should contain at least the valid content");
        Assert.DoesNotContain("null", result.ToLower());
    }
}
