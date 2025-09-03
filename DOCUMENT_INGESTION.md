# 📄 RAG Document Ingestion - Instrukcja

## ✅ System załadowany!

Twój system RAG jest teraz gotowy do pracy z dokumentami w Elasticsearch. Zaindeksowaliśmy już **9 chunków** z 4 przykładowych dokumentów.

## 🗂 Jak dodawać dokumenty

### 1. Obsługiwane formaty plików

System automatycznie rozpoznaje i przetwarza:

- **📄 PDF**: `.pdf` (z użyciem iText7)
- **📝 Word**: `.docx`, `.doc` (DocumentFormat.OpenXml)
- **📊 Excel**: `.xlsx`, `.xls` (arkusze kalkulacyjne)
- **📋 Tekstowe**: `.txt`, `.md`, `.csv`

### 2. Lokalizacja dokumentów

Dodaj pliki do folderu:
```
<project-root>/data/documents/
```

### 3. Automatyczne przetwarzanie

Po dodaniu plików uruchom ingestion worker:

```bash
# Na Linux/macOS:
./scripts/ingestion-manager.sh run

# Na Windows:
.\scripts\ingestion-manager.ps1 -Command run

# Albo manualnie:
cd src/RAG.Ingestion.Worker
dotnet run
```

### 4. Sprawdzanie statusu

```bash
# Sprawdź liczbę dokumentów
curl -u elastic:elastic "http://localhost:9200/rag_documents/_count?pretty"

# Sprawdź przykładowy dokument
curl -u elastic:elastic "http://localhost:9200/rag_documents/_search?size=1&pretty"

# Wyszukaj po treści
curl -u elastic:elastic -X POST "http://localhost:9200/rag_documents/_search?pretty" \
  -H "Content-Type: application/json" \
  -d '{"query": {"match": {"content": "twoje zapytanie"}}}'
```

## 🔧 Konfiguracja

### Chunk Settings (appsettings.json):
- **ChunkSize**: 1000 znaków (optymalne dla LLM)
- **ChunkOverlap**: 100 znaków (zachowanie kontekstu)
- **ProcessOnStartup**: true (automatyczne przetwarzanie)

### Elasticsearch Settings:
- **URL**: http://localhost:9200
- **Credentials**: elastic/elastic
- **Index**: rag_documents

## 📁 Struktura danych

Każdy dokument zostaje podzielony na chunki zawierające:

```json
{
  "id": "unique_chunk_id",
  "documentId": "parent_document_id", 
  "fileName": "document.pdf",
  "content": "tekst chunka...",
  "fileType": "PDF",
  "chunkIndex": 0,
  "embedding": [0.123, -0.456, ...], // 768-dim vector
  "createdAt": "2024-08-24T21:06:55Z",
  "metadata": {
    "parser": "PdfDocumentParser",
    "chunkLength": 950
  }
}
```

## 🚀 Przykłady użycia

### Dodaj dokumenty PDF/Word:
1. Skopiuj pliki do `/data/documents/`
2. Uruchom: `./scripts/ingestion-manager.sh run`
3. Worker automatycznie przetworzy nowe pliki

### Sprawdź wyniki wyszukiwania:
```bash
# Test wyszukiwania API
curl -u elastic:elastic -X POST "http://localhost:9200/rag_documents/_search" \
  -H "Content-Type: application/json" \
  -d '{"query": {"match": {"content": "API endpoints"}}}'

# Test wyszukiwania o Elasticsearch
curl -u elastic:elastic -X POST "http://localhost:9200/rag_documents/_search" \
  -H "Content-Type: application/json" \
  -d '{"query": {"match": {"content": "vector search"}}}'
```

## 🎯 Gotowe do produkcji!

Twój system RAG może teraz:
- ✅ Automatycznie parsować dokumenty Office i PDF
- ✅ Dzielić na optymalne chunki z overlapem
- ✅ Generować embeddingi (obecnie mock, do zastąpienia prawdziwym modelem)
- ✅ Indeksować w Elasticsearch z vector search
- ✅ Wyszukiwać dokumenty przez treść
- ✅ Integrować z Ollama LLM w RAG pipeline

**Następny krok**: Zamień mock embedding service na prawdziwy model (np. sentence-transformers)!
