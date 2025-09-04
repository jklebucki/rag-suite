# 🚀 Instrukcja Wdrożenia - Poprawki Rekonstrukcji Dokumentów

## 📋 Podsumowanie Zmian

### Główne Problemy Rozwiązane:
1. **Błędne wartości TotalChunks** w logach (540/18 zamiast 540/540)
2. **Brak informacji o długości zrekonstruowanej treści**
3. **Potencjalne puste chunki** nie były wykrywane
4. **Mapowanie Elasticsearch** mogło powodować błędy zapytań

### Zaimplementowane Poprawki:
- ✅ Ulepszone logowanie diagnostyczne
- ✅ Naprawiona logika porównania TotalChunks
- ✅ Filtrowanie pustych chunków przed rekonstrukcją
- ✅ Alternatywne zapytania Elasticsearch (fallback dla .keyword)
- ✅ Szczegółowe debugowanie zapytań i treści

## 🔧 Kroki Wdrożenia

### 1. **Backup i Przygotowanie**
```bash
# Zatrzymaj serwis
sudo systemctl stop rag-api

# Backup aktualnej wersji
sudo cp -r /opt/rag-suite /opt/rag-suite.backup.$(date +%Y%m%d_%H%M%S)
```

### 2. **Wdrożenie Nowej Wersji**
```bash
# Skopiuj nowe pliki
sudo cp -r src/RAG.Orchestrator.Api/bin/Release/net8.0/* /opt/rag-suite/

# Uprawnienia
sudo chown -R rag-user:rag-user /opt/rag-suite
sudo chmod +x /opt/rag-suite/RAG.Orchestrator.Api
```

### 3. **Uruchomienie Diagnostyki (PRZED startem serwisu)**
```bash
# Skopiuj skrypt diagnostyczny
sudo cp diagnose-elasticsearch.sh /opt/rag-suite/
sudo chmod +x /opt/rag-suite/diagnose-elasticsearch.sh

# Uruchom diagnostykę
cd /opt/rag-suite
./diagnose-elasticsearch.sh

# Sprawdź wyniki - szczególnie:
# - Czy chunki mają treść (Content length > 0)
# - Czy mapowanie sourceFile.keyword istnieje
# - Czy są unikalne pliki źródłowe
```

### 4. **Start Serwisu i Monitoring**
```bash
# Uruchom serwis
sudo systemctl start rag-api

# Monitoruj logi w czasie rzeczywistym
sudo journalctl -u rag-api -f | grep -E "(Reconstructing|Successfully reconstructed|WARNING|TotalChunks mismatch)"
```

## 📊 Co Monitorować w Logach

### ✅ Pozytywne Sygnały:
```
[INF] Successfully reconstructed full document with 540/540 chunks for document.docx. Content length: 125000 characters
[INF] Successfully fetched 540/540 chunks for document document.docx
```

### ⚠️ Ostrzeżenia do Zbadania:
```
[WRN] TotalChunks mismatch for document.docx: original chunk says 18, fetched chunks say 540, actual count: 540
[WRN] High number of empty chunks detected: 50/540 for document.docx
```

### 🚨 Błędy Krytyczne:
```
[WRN] WARNING: Reconstructed content is empty! Chunks found: 540, First chunk content length: 0
[WRN] All fetched chunks for document.docx have empty content! Falling back to matching chunks.
```

## 🧪 Test Weryfikacyjny

### Wykonaj zapytanie testowe:
```
Zapytanie: "wymień osoby i ich funkcje w analizuj Notatka ze spotkania - WinSped - 20240422"
```

### Oczekiwany rezultat w logach:
1. `[INF] Starting search for query: '...' with limit: 3`
2. `[INF] Found X chunks from Y total hits`
3. `[INF] Reconstructing document from X chunks for file: ...WinSped...`
4. `[INF] Successfully fetched 540/540 chunks for document ...`
5. `[INF] Successfully reconstructed full document with 540/540 chunks ... Content length: XXXX characters`

## 🔄 Plan B - Rollback

Jeśli coś pójdzie nie tak:

```bash
# Zatrzymaj serwis
sudo systemctl stop rag-api

# Przywróć backup
sudo rm -rf /opt/rag-suite
sudo cp -r /opt/rag-suite.backup.YYYYMMDD_HHMMSS /opt/rag-suite

# Uruchom ponownie
sudo systemctl start rag-api
```

## 📞 Support

Jeśli wystąpią problemy:
1. **Zapisz logi błędów** z `journalctl -u rag-api --since="10 minutes ago"`
2. **Uruchom diagnostykę** z `./diagnose-elasticsearch.sh`
3. **Sprawdź status Elasticsearch** z `curl -u elastic:password http://localhost:9200/_cluster/health`

---

**Uwaga:** Ta wersja zawiera dodatkowe logowanie diagnostyczne. W przypadku sukcesu można później zmniejszyć poziom logowania z `Debug` na `Information` w `appsettings.json`.
