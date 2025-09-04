# 🔍 Diagnostyka Rekonstrukcji Dokumentów - Przewodnik

## Problem

Podczas analizy logów serwera produkcyjnego zaobserwowano, że pomimo pozornie prawidłowej rekonstrukcji dokumentów (logi pokazują "Successfully reconstructed full document with 540/18 chunks"), faktyczna treść może nie być przekazywana do systemu RAG.

## 📋 Symptomy


1. **Logs wykazują nieprawidłowe wartości:**

   ```
   Successfully reconstructed full document with 540/18 chunks for X:\Citronex\IFS Materialy\Instrukcje\Notatka ze spotkania - WinSped - 20240422.docx
   ```
   * 540 chunków faktycznie pobranych
   * 18 chunków oczekiwanych (nieprawidłowa wartość `TotalChunks`)
2. **Chat RAG może otrzymywać pustą treść**
3. **Brak szczegółowych informacji o długości zrekonstruowanej treści**

## 🎯 **DIAGNOZA PROBLEMU - ROZWIĄZANA!**

**Po wykonaniu skryptu diagnostycznego na produkcji:**

### Faktyczny Stan:

* ✅ **540 chunków w Elasticsearch** dla pliku WinSped
* ✅ **18 unikalnych chunkIndex** (0-17)
* ❌ **30 duplikatów każdego chunka!**
* ✅ Wszystkie mają prawidłowy `totalChunks: 18`

### Przyczyna:

**WIELOKROTNE PRZETWARZANIE DOKUMENTU** - plik był indeksowany 30 razy, tworząc po 30 kopii każdego z 18 chunków.

### Dlaczego Log Pokazuje "540/18":

* System pobiera wszystkie 540 rekordów z Elasticsearch
* `TotalChunks` w pierwszym chunku = 18 (prawidłowo)
* **Nasz kod POPRAWNIE radzi sobie z duplikatami!**

## � Wyniki Rzeczywistej Diagnostyki Produkcji



**Data diagnostyki**: 2025-09-04**Serwer**: http://192.168.21.13:9200**Indeks**: `rag-chunks`

### Test Results:

```json
{
  "total_documents": 14518,
  "index_size": "98MB",
  "problematic_file": "Notatka ze spotkania - WinSped - 20240422.docx",
  "chunks_in_elasticsearch": 540,
  "unique_chunk_indices": 18,
  "duplicates_per_chunk": 30,
  "total_chunks_field_value": 18
}
```

### Chunk Distribution Analysis:

```json
{
  "chunk_indices": [0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17],
  "copies_per_index": 30,
  "total_chunks_consistency": "All chunks report totalChunks: 18"
}
```

### Wnioski:


1. ✅ **Mapowanie Elasticsearch jest prawidłowe** - `sourceFile.keyword` istnieje
2. ❌ **Dokument był przetwarzany wielokrotnie** (30 razy)
3. ✅ **Nasz kod rekonstrukcji radzi sobie z duplikatami**
4. ✅ **Wartości TotalChunks są spójne** (wszystkie = 18)

## �🛠️ Implementowane Poprawki

### 1. **Ulepszone Logowanie Diagnostyczne**

Dodano szczegółowe logi w `SearchService.cs`:

```csharp
// Debug długości treści chunków
_logger.LogDebug("Chunk content analysis - Total content length: {TotalLength}, Empty chunks: {EmptyCount}/{TotalCount}, Avg chunk size: {AvgSize}", 
    totalContentLength, emptyChunks, allChunks.Count, chunkContentLengths.Count > 0 ? chunkContentLengths.Average() : 0);

// Ostrzeżenie o pustej treści
if (reconstructedContent.Length == 0)
{
    _logger.LogWarning("WARNING: Reconstructed content is empty! Chunks found: {ChunkCount}, First chunk content length: {FirstChunkLength}", 
        allChunks.Count, allChunks.FirstOrDefault()?.Content?.Length ?? 0);
}
```

### 2. **Naprawiona Logika Porównania TotalChunks**

```csharp
// Używaj rzeczywistej wartości TotalChunks z pobranych chunków
var expectedChunks = allChunks.FirstOrDefault()?.TotalChunks ?? firstChunk.TotalChunks;

// Log rozbieżności w wartościach
if (firstChunk.TotalChunks != expectedChunks)
{
    _logger.LogWarning("TotalChunks mismatch for {SourceFile}: original chunk says {OriginalTotal}, fetched chunks say {FetchedTotal}, actual count: {ActualCount}", 
        sourceFile, firstChunk.TotalChunks, expectedChunks, actualChunks);
}
```

### 3. **Filtrowanie Pustych Chunków**

```csharp
// Filtruj chunki z pustą treścią przed rekonstrukcją
var validChunks = allChunks.Where(c => !string.IsNullOrWhiteSpace(c.Content)).ToList();

if (validChunks.Count == 0)
{
    _logger.LogWarning("All fetched chunks for {SourceFile} have empty content! Falling back to matching chunks.", sourceFile);
    reconstructedContent = string.Join("\n\n", sortedChunks.Select(c => c.Content));
}
```

### 4. **Alternatywne Zapytania Elasticsearch**

Dodano fallback gdy `.keyword` field nie istnieje:

```csharp
// Pierwsze zapytanie z .keyword
"sourceFile.keyword": sourceFile

// Fallback bez .keyword jeśli pierwsze nie działa
if (errorContent.Contains("field [sourceFile.keyword]"))
{
    "sourceFile": sourceFile // Bez .keyword
}
```

### 5. **Debug Logowanie Zapytań**

```csharp
_logger.LogDebug("Fetching all chunks query for {SourceFile}: {Query}", sourceFile, json);
```

## 🔍 Narzędzia Diagnostyczne

### Skrypt Bash: `diagnose-elasticsearch.sh`

```bash
# Uruchom diagnostykę
./diagnose-elasticsearch.sh

# Lub z własnymi parametrami
ES_HOST="http://192.168.21.13:9200" ES_USER="elastic" ES_PASS="password" ./diagnose-elasticsearch.sh
```

### Skrypt PowerShell: `diagnose-elasticsearch.ps1`

```powershell
# Uruchom diagnostykę lokalnie
.\diagnose-elasticsearch.ps1

# Lub zdalnie na serwerze produkcyjnym
.\diagnose-elasticsearch.ps1 -ElasticsearchUrl "http://192.168.21.13:9200" -Username "elastic" -Password "yourpassword"
```

## 📊 Testy Diagnostyczne

Skrypty wykonują następujące testy:

### Test 1: Przegląd Indeksu

* Liczba dokumentów
* Rozmiar indeksu

### Test 2: Przykładowe Dokumenty

* Struktura 3 losowych dokumentów
* Długość treści chunków

### Test 3: Test Konkretnego Dokumentu

* Zapytanie z `sourceFile.keyword`
* Zapytanie z `sourceFile` (bez .keyword)
* Zapytanie z `match`

### Test 4: Mapowanie Indeksu

* Struktura pól `sourceFile`, `content`, `position`
* Typ mapowania

### Test 5: Unikalne Pliki Źródłowe

* Lista 10 najczęstszych plików
* Liczba chunków na plik

## 🚨 Co Sprawdzić w Logach

Po wdrożeniu nowej wersji, szukaj w logach:

### Prawidłowe Rekonstrukcje:

```
[INF] Successfully reconstructed full document with 540/540 chunks for document.docx. Content length: 125000 characters
```

### Problemy z TotalChunks:

```
[WRN] TotalChunks mismatch for document.docx: original chunk says 18, fetched chunks say 540, actual count: 540
```

### Puste Chunki:

```
[WRN] High number of empty chunks detected: 50/540 for document.docx
[WRN] WARNING: Reconstructed content is empty! Chunks found: 540, First chunk content length: 0
```

### Problemy z Mapowaniem:

```
[WRN] Failed to fetch all chunks for document.docx. Status: 400. Error: field [sourceFile.keyword] not found
[INF] Retrying fetch with alternative query (without .keyword) for document.docx
```

## 🔧 Możliwe Przyczyny Problemów

### 1. **Nieprawidłowe Mapowanie Elasticsearch**

* `sourceFile` nie jest typu `keyword`
* Brak pola `sourceFile.keyword`

### 2. **Puste Chunki w Indeksie**

* Chunki zostały zaindeksowane bez treści
* Problem z ekstraktorem tekstu

### 3. **Nieprawidłowe Wartości TotalChunks**

* Nie aktualizowane podczas indeksowania
* Różne wartości w różnych chunkach tego samego dokumentu

### 4. **Problemy z Kodowaniem**

* Znaki specjalne w ścieżkach plików
* Różne formaty ścieżek (Windows vs Linux)

## 📋 Plan Działania


1. **Uruchom skrypt diagnostyczny** na serwerze produkcyjnym
2. **Wdróż nową wersję** z ulepszonym logowaniem
3. **Monitoruj logi** podczas następnych zapytań
4. **Sprawdź mapowanie Elasticsearch** i w razie potrzeby napraw
5. **Ponownie zaindeksuj** dokumenty jeśli chunki mają pustą treść

## 🎯 Oczekiwane Rezultaty

Po poprawkach system powinien:

* Prawidłowo rekonstruować dokumenty z wszystkich chunków
* Logować rzeczywistą długość zrekonstruowanej treści
* Ostrzegać o problemach z pustymi chunkami
* Automatycznie radzić sobie z różnymi formatami mapowania Elasticsearch
* Dostarczać pełny kontekst do systemu RAG chat


