using RAG.Orchestrator.Api.Features.Reconstruction;
using RAG.Orchestrator.Api.Features.Search;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace RAG.Orchestrator.Api.Tests.Features.Reconstruction;

public class WinSpedDocumentReconstructionTests
{
    private readonly DocumentReconstructionService _service;
    private readonly Mock<ILogger<DocumentReconstructionService>> _loggerMock;

    public WinSpedDocumentReconstructionTests()
    {
        _loggerMock = new Mock<ILogger<DocumentReconstructionService>>();
        _service = new DocumentReconstructionService(_loggerMock.Object);
    }

    [Fact]
    public void ReconstructDocument_WinSpedStyleOverlap_RemovesRepeatedContent()
    {
        // Arrange - przykład podobny do dokumentu WinSped z overlap'em
        var chunks = new List<ChunkInfo>
        {
            new ChunkInfo
            {
                ChunkIndex = 0,
                Content = @"NOTATKA ZE SPOTKANIA Temat spotkania: Integracja z WinSped Data i czas spotkania: 2024-04-22 13:00 – 15:00 Miejsce spotkania: MS Teams Notatkę opracował/a: Aneta Kocot Notatkę zatwierdza: Uczestnicy po stronie CITRONEX I: Jarosław Kłębucki, Kacper Kozłowski, Grażyna Aniszkiewicz, Anna Krakowiak, Małgorzata Brzezińska, Jakub Bryndzia, Arkadiusz Olszówki, Marek Szulc Uczestnicy po stronie DELEGATE:"
            },
            new ChunkInfo
            {
                ChunkIndex = 1,
                Content = @"Bryndzia, Arkadiusz Olszówki, Marek Szulc Uczestnicy po stronie DELEGATE:  Jakub Bryndzia, Arkadiusz Olszówki, Marek Szulc Uczestnicy po stronie DELEGATE: Aneta Kocot Wersja notatki: 1 Spis treści TOC \o ""1-3"" \h \z \u I.Cel spotkania PAGEREF _Toc164694248 \h 1 II.Zagadnienia poruszane w czasie spotkania PAGEREF _Toc164694249 \h 1"
            },
            new ChunkInfo
            {
                ChunkIndex = 2,
                Content = @"PAGEREF _Toc164694252 \h 7 Cel spotkania Prezentacja integracji IFS – WINSPED. Zagadnienia poruszane w czasie spotkania Podczas spotkania omówiona została wymagana parametryzacja: Przypisanie numeru z TMS na umiejscowieniu: Aby możliwe było decydowanie o tym, które zamówienia mają być planowane przez system TMS na typie zamówienia klienta dodany jest parametr."
            },
            new ChunkInfo
            {
                ChunkIndex = 3,
                Content = @"jest parametr. Na bazie TEST założyłam nowy typ dedykowany do testów TMS.  odany jest parametr. Na bazie TEST założyłam nowy typ dedykowany do testów TMS. Ważne jest aby dla zamówień obsługiwanych przez TMS typ wysyłki był NA (Nie automatyczny), ponieważ wysyłki te tworzone będą w ramach integracji, a co za tym idzie nie chcemy, żeby IFS utworzył je automatycznie."
            }
        };

        // Act
        var result = _service.ReconstructDocument(chunks, removeDuplicates: true);

        // Assert
        // Sprawdź czy powtarzające się fragmenty zostały usunięte
        var duplicateCount = CountOccurrences(result, "Uczestnicy po stronie DELEGATE:");
        Assert.True(duplicateCount <= 2, $"Found {duplicateCount} occurrences of 'Uczestnicy po stronie DELEGATE:', expected <= 2");

        duplicateCount = CountOccurrences(result, "Na bazie TEST założyłam nowy typ dedykowany do testów TMS");
        Assert.True(duplicateCount <= 1, $"Found {duplicateCount} occurrences of 'Na bazie TEST założyłam nowy typ dedykowany do testów TMS', expected <= 1");

        duplicateCount = CountOccurrences(result, "PAGEREF _Toc164694252");
        Assert.True(duplicateCount <= 1, $"Found {duplicateCount} occurrences of 'PAGEREF _Toc164694252', expected <= 1");

        // Sprawdź czy treść jest kompletna
        Assert.Contains("NOTATKA ZE SPOTKANIA", result);
        Assert.Contains("Cel spotkania", result);
        Assert.Contains("Prezentacja integracji IFS – WINSPED", result);
        Assert.Contains("parametryzacja", result);

        // Sprawdź długość - powinna być mniejsza niż suma wszystkich chunków
        var totalChunkLength = chunks.Sum(c => c.Content?.Length ?? 0);
        Assert.True(result.Length < totalChunkLength, 
            $"Reconstructed length ({result.Length}) should be less than total chunk length ({totalChunkLength})");
    }

    [Fact]
    public void ReconstructDocument_PAGEREFOverlap_RemovesTableOfContentsOverlap()
    {
        // Arrange - specjalny przypadek z PAGEREF który często się powtarza
        var chunks = new List<ChunkInfo>
        {
            new ChunkInfo
            {
                ChunkIndex = 0,
                Content = "I.Cel spotkania PAGEREF _Toc164694248 \\h 1 II.Zagadnienia poruszane w czasie spotkania PAGEREF _Toc164694249 \\h 1 III.Zadania do wykonania PAGEREF _Toc164694250 \\h 6"
            },
            new ChunkInfo
            {
                ChunkIndex = 1,
                Content = "PAGEREF _Toc164694250 \\h 6 IV.Termin następnego spotkania PAGEREF _Toc164694251 \\h 7 V.Podsumowanie PAGEREF _Toc164694252 \\h 7 Cel spotkania Prezentacja integracji IFS – WINSPED."
            }
        };

        // Act
        var result = _service.ReconstructDocument(chunks);

        // Assert
        var pagerefCount = CountOccurrences(result, "PAGEREF _Toc164694250");
        Assert.True(pagerefCount <= 1, $"Found {pagerefCount} occurrences of 'PAGEREF _Toc164694250', expected <= 1");

        Assert.Contains("I.Cel spotkania", result);
        Assert.Contains("V.Podsumowanie", result);
        Assert.Contains("Prezentacja integracji IFS – WINSPED", result);
    }

    [Fact]
    public void ReconstructDocument_RepeatedPhrases_RemovesRepeatedPhrases()
    {
        // Arrange - przykład z powtarzającymi się frazami
        var chunks = new List<ChunkInfo>
        {
            new ChunkInfo
            {
                ChunkIndex = 0,
                Content = "harmonogram 1308 Na potrzeby testów harmonogram wywoływany jest ręcznie."
            },
            new ChunkInfo
            {
                ChunkIndex = 1,
                Content = "został harmonogram 1308 Na potrzeby testów harmonogram wywoływany jest ręcznie. Docelowo powinien być ustawiony co określony przedział czasu, np. co pół godziny."
            }
        };

        // Act
        var result = _service.ReconstructDocument(chunks);

        // Assert
        var phraseCount = CountOccurrences(result, "harmonogram wywoływany jest ręcznie");
        Assert.True(phraseCount <= 1, $"Found {phraseCount} occurrences of 'harmonogram wywoływany jest ręcznie', expected <= 1");

        Assert.Contains("harmonogram 1308", result);
        Assert.Contains("co pół godziny", result);
    }

    private static int CountOccurrences(string text, string pattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
            return 0;

        int count = 0;
        int startIndex = 0;

        while ((startIndex = text.IndexOf(pattern, startIndex, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            startIndex += pattern.Length;
        }

        return count;
    }
}
