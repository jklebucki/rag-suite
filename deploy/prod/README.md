# RAG-Suite Production Deployment Scripts

## Skrypty reinstalacji serwisów

### 1. Reinstalacja tylko serwisu embedding
```bash
# Linux/macOS
chmod +x restart-embedding.sh
./restart-embedding.sh

# Windows PowerShell
.\restart-embedding.ps1
```

### 2. Pełna reinstalacja wszystkich serwisów
```bash
# Linux/macOS
chmod +x reinstall-all.sh
./reinstall-all.sh
```

## Co robią skrypty

### restart-embedding.sh / restart-embedding.ps1
- Zatrzymuje serwis embedding
- Usuwa stary kontener
- Pobiera najnowszy obraz
- Uruchamia z nową konfiguracją (auto-truncate + max_input_length: 512)
- Sprawdza czy serwis działa

### reinstall-all.sh
- Zatrzymuje wszystkie serwisy (Elasticsearch + embedding)
- Usuwa wszystkie kontenery
- Pobiera najnowsze obrazy
- Uruchamia wszystkie serwisy
- Sprawdza status każdego serwisu

## Nowa konfiguracja embedding service

Po wykonaniu skryptu serwis embedding będzie miał:
- `max_input_length`: 512 tokenów (było 256)
- `auto_truncate`: true (było false)
- Obsługa większych chunków tekstowych

## Sprawdzanie statusu

```bash
# Status kontenerów
docker ps

# Logi
docker logs embedding-service-srv
docker logs es-srv

# Konfiguracja embedding
curl http://localhost:8580/info

# Status Elasticsearch
curl -u elastic:elastic http://localhost:9200/_cluster/health
```

## Po reinstalacji

Po pomyślnej reinstalacji możesz:
1. Uruchomić RAG.Collector z większymi chunkami
2. Chunki do 400 znaków będą przetwarzane bez błędów 413
3. Auto-truncate zapewni obsługę nawet większych tekstów
