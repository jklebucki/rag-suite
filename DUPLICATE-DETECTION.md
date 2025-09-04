# Inteligentny Mechanizm Wykrywania Zmian w Plikach - RAG.Collector

## Opis problemu

System RAG.Collector miał problem z **duplikacją dokumentów** w indeksie Elasticsearch. Ten sam dokument był indeksowany wielokrotnie, co powodowało:

- Masywną duplikację chunks w indeksie (np. 540 identycznych rekordów dla jednego dokumentu)
- Nieprawidłowe działanie rekonstrukcji dokumentów w wyszukiwaniu
- Nadmierne zużycie miejsca w Elasticsearch
- Powolne odpowiedzi API ze względu na przetwarzanie duplikatów

## Rozwiązanie

Zaimplementowałem **inteligentny mechanizm wykrywania zmian** który:

### 1. Sprawdza czy plik wymaga reindeksacji
- Generuje hash SHA-256 całego pliku
- Porównuje z ostatnio zaindeksowaną wersją
- Sprawdza datę modyfikacji pliku
- **Pomija reindeksację** jeśli plik się nie zmienił

### 2. Usuwa stare dane przed indeksacją nowych
- Przed indeksacją nowej wersji usuwa wszystkie chunks starej wersji
- Zapobiega akumulacji duplikatów w indeksie

### 3. Przechowuje metadane plików
- Zapisuje informacje o zaindeksowanych plikach
- Śledzi hash, datę modyfikacji i liczbę chunks

## Implementacja

### Nowe komponenty:

#### `IFileChangeDetectionService` & `FileChangeDetectionService`
```csharp
// Sprawdza czy plik wymaga reindeksacji
Task<bool> ShouldReindexFileAsync(string filePath, string fileHash, DateTime lastModified, CancellationToken cancellationToken = default);

// Zapisuje informacje o zaindeksowanym pliku
Task RecordIndexedFileAsync(string filePath, string fileHash, DateTime lastModified, int chunkCount, CancellationToken cancellationToken = default);
```

#### Rozszerzone modele:
- `FileItem` - dodane pole `FileHash`
- `TextChunk` - dodane pole `FileHash`

#### Zmodyfikowane serwisy:
- `CollectorWorker` - generuje hash pliku podczas ekstrakcji
- `ChunkingService` - przekazuje hash pliku do chunks
- `IndexingService` - używa mechanizmu wykrywania zmian

### Algorytm działania:

1. **Skanowanie pliku**:
   ```csharp
   fileItem.FileHash = await GenerateFileHashAsync(fileItem.Path, cancellationToken);
   ```

2. **Sprawdzenie czy wymaga reindeksacji**:
   ```csharp
   var needsReindexing = await _fileChangeDetection.ShouldReindexFileAsync(
       sourceFile, fileHash, lastModified, cancellationToken);
   
   if (!needsReindexing) {
       return 0; // Pomiń indeksację
   }
   ```

3. **Usunięcie starych chunks**:
   ```csharp
   await _elasticsearchService.DeleteDocumentsBySourceFileAsync(sourceFile, cancellationToken);
   ```

4. **Indeksacja nowych chunks**:
   ```csharp
   var indexedCount = await IndexChunksBatchAsync(fileChunks, cancellationToken);
   ```

5. **Zapisanie metadanych**:
   ```csharp
   await _fileChangeDetection.RecordIndexedFileAsync(
       sourceFile, fileHash, lastModified, indexedCount, cancellationToken);
   ```

## Korzyści

### ✅ Eliminacja duplikatów
- Każdy plik jest indeksowany tylko raz (chyba że się zmieni)
- Automatyczne usuwanie starych wersji przed indeksacją nowych

### ✅ Optymalizacja wydajności
- Pomijanie niezmiennych plików oszczędza czas i zasoby
- Szybsze skanowanie dużych folderów

### ✅ Oszczędność miejsca
- Brak duplikatów w Elasticsearch
- Dokładna kontrola nad tym co jest indeksowane

### ✅ Poprawne działanie wyszukiwania
- Rekonstrukcja dokumentów działa prawidłowo
- Brak wielokrotnych wyników dla tego samego dokumentu

## Testowanie

Po implementacji mechanizmu:

1. **Usunąłem duplikaty**:
   ```bash
   curl -X POST "http://192.168.21.13:9200/rag-chunks/_delete_by_query" \
     -u "elastic:elastic" \
     -d '{"query": {"term": {"sourceFile.keyword": "ścieżka_do_pliku"}}}'
   ```

2. **Zresetowałem system** - teraz każdy plik będzie indeksowany tylko raz

3. **Zweryfikowałem działanie** - system prawidłowo wykrywa zmiany w plikach

## Konfiguracja

System jest automatycznie aktywny po:
- Zbudowaniu projektu (`dotnet build`)
- Uruchomieniu RAG.Collector

Nie wymaga dodatkowej konfiguracji - działa "out of the box".

## Logs

System loguje działanie mechanizmu:
```
[INFO] Checking if file needs reindexing: C:\path\to\file.docx
[INFO] File unchanged, skipping indexing: C:\path\to\file.docx
```

lub

```
[INFO] File content changed (hash mismatch), needs reindexing: C:\path\to\file.docx
[INFO] Deleted 18 existing chunks for file: C:\path\to\file.docx
[INFO] Completed indexing file: 18/18 chunks indexed
```

Mechanizm rozwiązuje problem duplikatów i znacznie poprawia efektywność systemu RAG.
