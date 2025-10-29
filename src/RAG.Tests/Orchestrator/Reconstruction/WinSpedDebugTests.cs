using RAG.Orchestrator.Api.Features.Reconstruction;
using RAG.Orchestrator.Api.Features.Search;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace RAG.Tests.Orchestrator.Reconstruction;

public class WinSpedDebugTests
{
    private readonly DocumentReconstructionService _service;
    private readonly Mock<ILogger<DocumentReconstructionService>> _loggerMock;
    private readonly ITestOutputHelper _output;

    public WinSpedDebugTests(ITestOutputHelper output)
    {
        _loggerMock = new Mock<ILogger<DocumentReconstructionService>>();
        _service = new DocumentReconstructionService(_loggerMock.Object);
        _output = output;
    }

    [Fact]
    public void Debug_WinSpedProblematicChunks()
    {
        // Arrange - tylko problematyczne chunki 3 i 4
        var chunk3 = @"PAGEREF _Toc164694252 \h 7 Cel spotkania Prezentacja integracji IFS – WINSPED. Zagadnienia poruszane w czasie spotkania Podczas spotkania omówiona została wymagana parametryzacja: Przypisanie numeru z TMS na umiejscowieniu: Aby możliwe było decydowanie o tym, które zamówienia mają być planowane przez system TMS na typie zamówienia klienta dodany jest parametr.";
        
        var chunk4 = @"jest parametr. Na bazie TEST założyłam nowy typ dedykowany do testów TMS.  odany jest parametr. Na bazie TEST założyłam nowy typ dedykowany do testów TMS. Ważne jest aby dla zamówień obsługiwanych przez TMS typ wysyłki był NA (Nie automatyczny), ponieważ wysyłki te tworzone będą w ramach integracji, a co za tym idzie nie chcemy, żeby IFS utworzył je automatycznie.";

        var chunks = new List<ChunkInfo>
        {
            new ChunkInfo { ChunkIndex = 0, Content = chunk3 },
            new ChunkInfo { ChunkIndex = 1, Content = chunk4 }
        };

        // Act
        var result = _service.ReconstructDocument(chunks);
        
        _output.WriteLine($"Chunk 3: '{chunk3}'");
        _output.WriteLine($"Chunk 4: '{chunk4}'");
        _output.WriteLine($"Result: '{result}'");

        // Count occurrences
        var count = CountOccurrences(result, "Na bazie TEST założyłam nowy typ dedykowany do testów TMS");
        _output.WriteLine($"Occurrences of 'Na bazie TEST założyłam nowy typ dedykowany do testów TMS': {count}");

        // Test individual overlap removal
        var overlapResult = _service.RemoveOverlap(chunk3, chunk4, 200);
        _output.WriteLine($"Direct overlap removal result: '{overlapResult}'");

        // Dla debugowania - sprawdźmy czy ogólnie tekst jest lepszy
        var originalLength = chunk3.Length + chunk4.Length;
        _output.WriteLine($"Original total length: {originalLength}, Result length: {result.Length}");
        
        // Główny test - czy usunięto jakikolwiek overlap (długość powinna być mniejsza)
        Assert.True(result.Length < originalLength, "Should remove some overlap");
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
