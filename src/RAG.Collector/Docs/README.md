# RAG.Collector

**Windows Service** do indeksowania plików z udziałów SMB/NTFS i publikowania chunków z embeddingami do Elasticsearch.

## Instalacja jako Windows Service

### Wymagania
- Windows Server 2019+ lub Windows 10/11
- .NET 8.0 Runtime
- Dostęp do udziałów SMB/NTFS
- Elasticsearch 8.x (opcjonalnie z autoryzacją)

### Kroki instalacji

1. **Kompilacja projektu**
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained false
   ```

2. **Utworzenie usługi**
   ```powershell
   sc create "RAG Collector" binPath= "C:\Services\RAG.Collector\RAG.Collector.exe" start= auto
   sc description "RAG Collector" "RAG document indexing service for Elasticsearch"
   ```

3. **Konfiguracja uprawnień**
   ```powershell
   # Uruchom jako konto sieciowe z dostępem do udziałów
   sc config "RAG Collector" obj= "DOMAIN\RAGServiceAccount" password= "password"
   ```

4. **Uruchomienie**
   ```powershell
   sc start "RAG Collector"
   ```

## Konfiguracja

### appsettings.Production.json
```json
{
  "Collector": {
    "SourceFolders": [
      "\\\\fileserver\\shares\\documents",
      "C:\\LocalDocuments"
    ],
    "FileExtensions": [".pdf", ".docx", ".xlsx", ".pptx", ".txt", ".csv", ".md"],
    "ChunkSize": 1200,
    "ChunkOverlap": 200,
    "ElasticsearchUrl": "https://elasticsearch.domain.com:9200",
    "ElasticsearchApiKey": "your-api-key",
    "AllowSelfSignedCert": false,
    "IndexName": "rag-chunks",
    "ProcessingIntervalMinutes": 30,
    "BulkBatchSize": 200
  }
}
```

### Parametry konfiguracji

| Parametr | Opis | Domyślna wartość |
|----------|------|------------------|
| `SourceFolders` | Lista folderów do skanowania (UNC/lokalne) | `[]` |
| `FileExtensions` | Obsługiwane rozszerzenia plików | `[".pdf", ".docx", ...]` |
| `ChunkSize` | Rozmiar chunka w znakach | `1200` |
| `ChunkOverlap` | Nakładanie chunków w znakach | `200` |
| `ElasticsearchUrl` | URL do Elasticsearch | `http://localhost:9200` |
| `ElasticsearchUsername` | Nazwa użytkownika (Basic Auth) | `null` |
| `ElasticsearchPassword` | Hasło (Basic Auth) | `null` |
| `ElasticsearchApiKey` | Klucz API (preferowany) | `null` |
| `AllowSelfSignedCert` | Akceptuj self-signed certyfikaty | `false` |
| `IndexName` | Nazwa indeksu ES | `rag-chunks` |
| `ProcessingIntervalMinutes` | Odstęp między cyklami | `60` |
| `BulkBatchSize` | Rozmiar batcha do ES | `200` |

## Logi

Logi są zapisywane w folderze `logs/` obok pliku wykonywalnego:
- `rag-collector-YYYYMMDD.txt` - logi dzienne
- Rotacja codziennie, przechowywanie 30 dni

## Monitorowanie

Service loguje kluczowe metryki:
- Liczba przetworzonych plików
- Liczba wygenerowanych chunków  
- Czas przetwarzania
- Błędy indeksacji

## Troubleshooting

### Typowe problemy

1. **Brak dostępu do udziałów**
   - Sprawdź uprawnienia konta usługi
   - Zweryfikuj połączenie sieciowe

2. **Błędy Elasticsearch**
   - Sprawdź łączność z `telnet elasticsearch-host 9200`
   - Zweryfikuj credentials/API key

3. **Problemy z chunkowaniem**
   - Sprawdź rozmiary plików
   - Zweryfikuj obsługiwane formaty

### Komendy diagnostyczne

```powershell
# Status usługi
sc query "RAG Collector"

# Logi Windows Event Log
Get-EventLog -LogName Application -Source "RAG Collector"

# Test łączności z ES
Invoke-RestMethod -Uri "http://elasticsearch:9200/_cluster/health"
```

## Dalsze kroki

Ten projekt jest częścią większego ekosystemu RAG Suite. Po ukończeniu kroku 1, implementowane będą kolejne funkcjonalności:
- Ekstrakcja treści z różnych formatów plików
- Generowanie embeddingów ONNX
- Indeksacja do Elasticsearch z pełnym mappingiem
- Obsługa ACL i incremental updates
