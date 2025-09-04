# 📄 Rekonstrukcja Dokumentów z Chunków - Dokumentacja

## Przegląd

System RAG Suite posiada zaawansowany mechanizm rekonstrukcji całych dokumentów z chunków podczas wyszukiwania w Elasticsearch. Ta funkcjonalność zapewnia, że użytkownicy otrzymują pełny kontekst dokumentów w odpowiedziach na zapytania RAG.

## ✅ Zaimplementowane Funkcjonalności

### 1. **Automatyczne Grupowanie Chunków po Pliku Źródłowym**
- System automatycznie grupuje znalezione chunki według pola `sourceFile`
- Zachowuje porządek chunków według `position.chunkIndex`
- Sortuje rezultaty według najwyższego wyniku dopasowania na plik

### 2. **Inteligentna Rekonstrukcja Dokumentów**
- **Wykrywa dokumenty wielochunkowe**: Sprawdza czy `totalChunks > 1`
- **Pobiera wszystkie chunki**: Używa `FetchAllChunksForDocument()` do pobrania wszystkich części dokumentu
- **Waliduje kompletność**: Sprawdza czy pobrano wystarczającą liczbę chunków
- **Fallback na znalezione chunki**: Jeśli rekonstrukcja się nie powiedzie, używa tylko znalezionych chunków

### 3. **Konfigurowalność Rekonstrukcji**

W `ElasticsearchOptions` dodano nowe opcje konfiguracyjne:

```csharp
public class ElasticsearchOptions
{
    // ... istniejące opcje ...
    
    /// <summary>
    /// Określa czy zawsze rekonstruować pełne dokumenty z chunków
    /// </summary>
    public bool AlwaysReconstructFullDocuments { get; set; } = true;
    
    /// <summary>
    /// Minimalny procent chunków wymagany do rekonstrukcji (0.0-1.0)
    /// </summary>
    public double MinimumChunkCompleteness { get; set; } = 0.8;
    
    /// <summary>
    /// Maksymalna liczba chunków do pobrania na dokument
    /// </summary>
    public int MaxChunksPerDocument { get; set; } = 1000;
}
```

### 4. **Zaawansowane Zapytania Elasticsearch**

#### Pobieranie Wszystkich Chunków:
```json
{
  "query": {
    "term": {
      "sourceFile.keyword": "dokument.pdf"
    }
  },
  "size": 1000,
  "sort": [
    { "position.chunkIndex": { "order": "asc" } }
  ],
  "_source": ["content", "position", "sourceFile", "fileExtension", "indexedAt"]
}
```

### 5. **Walidacja Sekwencji Chunków**
- Sprawdza czy wszystkie oczekiwane chunki są dostępne
- Loguje ostrzeżenia o brakujących chunkach
- Waliduje kompletność sekwencji `0, 1, 2, ..., N-1`

### 6. **Ulepszone Logowanie i Monitoring**
- Szczegółowe logi procesu rekonstrukcji
- Monitoring kompletności dokumentów
- Ostrzeżenia o brakujących chunkach
- Informacje o wydajności rekonstrukcji

## 🔧 Struktura Danych Chunków

### ChunkDocument (Elasticsearch)
```csharp
public class ChunkDocument
{
    public string SourceFile { get; set; }        // Ścieżka do oryginalnego pliku
    public ChunkPositionInfo Position { get; set; } // Pozycja chunka w dokumencie
    // ... inne pola ...
}

public class ChunkPositionInfo
{
    public int ChunkIndex { get; set; }    // Numer chunka (0, 1, 2, ...)
    public int TotalChunks { get; set; }   // Całkowita liczba chunków w dokumencie
    public int StartIndex { get; set; }    // Pozycja początkowa w tekście
    public int EndIndex { get; set; }      // Pozycja końcowa w tekście
    public int? Page { get; set; }         // Numer strony (opcjonalnie)
    public string? Section { get; set; }   // Sekcja dokumentu (opcjonalnie)
}
```

## 🚀 Proces Rekonstrukcji

### Krok 1: Wyszukiwanie i Grupowanie
1. Wykonanie zapytania Elasticsearch z hybrid search (BM25 + kNN)
2. Grupowanie wyników według `sourceFile`
3. Sortowanie grup według najwyższego wyniku dopasowania

### Krok 2: Decyzja o Rekonstrukcji
```csharp
private bool ShouldReconstructFullDocument(List<ChunkInfo> chunks)
{
    // Sprawdź konfigurację
    if (!_options.AlwaysReconstructFullDocuments) return false;
    
    // Rekonstruuj jeśli mamy wiele chunków lub dokument jest wielochunkowy
    return chunks.Count > 1 || chunks.Any(c => c.TotalChunks > 1);
}
```

### Krok 3: Pobieranie Wszystkich Chunków
```csharp
private async Task<List<ChunkInfo>> FetchAllChunksForDocument(string sourceFile, CancellationToken cancellationToken)
{
    // Pobierz wszystkie chunki dla danego pliku
    // Posortuj według chunkIndex
    // Waliduj kompletność sekwencji
}
```

### Krok 4: Walidacja i Rekonstrukcja
```csharp
if (actualChunks >= expectedChunks * _options.MinimumChunkCompleteness)
{
    // Rekonstruuj pełny dokument
    reconstructedContent = string.Join("\n\n", allChunks.OrderBy(c => c.ChunkIndex).Select(c => c.Content));
}
else
{
    // Fallback - użyj tylko znalezionych chunków
    reconstructedContent = string.Join("\n\n", sortedChunks.Select(c => c.Content));
}
```

## 📊 Metadane Wyników

Każdy zrekonstruowany dokument zawiera metadane:

```json
{
  "metadata": {
    "category": "PDF",
    "score": 0.95,
    "index": "rag-chunks",
    "chunksFound": 3,
    "totalChunks": 5,
    "reconstructed": true,
    "highlights": "fragment 1 ... fragment 2 ..."
  }
}
```

## ⚙️ Konfiguracja

### appsettings.json
```json
{
  "Services": {
    "Elasticsearch": {
      "Url": "http://localhost:9200",
      "AlwaysReconstructFullDocuments": true,
      "MinimumChunkCompleteness": 0.8,
      "MaxChunksPerDocument": 1000
    }
  }
}
```

### Opcje Konfiguracyjne:

| Opcja | Typ | Domyślna | Opis |
|-------|-----|----------|------|
| `AlwaysReconstructFullDocuments` | bool | true | Czy zawsze rekonstruować pełne dokumenty |
| `MinimumChunkCompleteness` | double | 0.8 | Minimalny procent chunków (80%) |
| `MaxChunksPerDocument` | int | 1000 | Maksymalna liczba chunków na dokument |

## 🔍 Przykłady Użycia

### Zapytanie: "procedury bezpieczeństwa"

**Bez rekonstrukcji:**
- Zwraca tylko fragmenty zawierające frazę
- Brak pełnego kontekstu

**Z rekonstrukcją:**
- Zwraca cały dokument "Instrukcja_Bezpieczenstwa.pdf"
- Pełny kontekst z wszystkich 8 chunków
- Highlights z oryginalnych dopasowań

### Monitorowanie w Logach
```
[INF] Reconstructing document from 3 chunks for file: document.pdf
[INF] Successfully fetched 8/8 chunks for document document.pdf
[INF] Successfully reconstructed full document with 8/8 chunks for document.pdf
```

## 🎯 Korzyści

1. **Pełny Kontekst**: Użytkownicy otrzymują kompletne dokumenty zamiast fragmentów
2. **Zachowanie Struktury**: Kolejność chunków jest zachowana
3. **Inteligentny Fallback**: System gracefully degraduje gdy nie można zrekonstruować
4. **Wysoka Wydajność**: Konfigurowalność pozwala na optymalizację
5. **Dokładne Highlights**: Zachowane są oryginalne podświetlenia dopasowań

## 🚨 Uwagi Bezpieczeństwa

- Rekonstrukcja respektuje ACL (Access Control Lists) z chunków
- Wszystkie chunki muszą mieć ten sam poziom dostępu
- Logowanie nie zawiera wrażliwych danych z dokumentów

## 📈 Wydajność

- Rekonstrukcja jest async/await z cancellation tokens
- Batch queries dla lepszej wydajności
- Konfigurowalne limity dla dużych dokumentów
- Fallback mechanizmy zapobiegają timeoutom
