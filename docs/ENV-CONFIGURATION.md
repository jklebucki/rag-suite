# RAG Suite - Przewodnik po plikach środowiskowych

## 📁 Dostępne pliki konfiguracyjne

### 1. `.env` (Domyślny)
**Przeznaczenie**: Główny plik konfiguracyjny dla standardowego developmentu  
**RAM**: Zoptymalizowany dla 16GB RAM  
**Model LLM**: `microsoft/DialoGPT-medium`  
**Alokacja pamięci**:
- Elasticsearch: 3GB
- LLM Service: 4GB
- System: ~9GB pozostałe

### 2. `.env.development`
**Przeznaczenie**: Środowisko development z debugowaniem  
**RAM**: Konserwatywne użycie (8GB całkowite)  
**Model LLM**: `microsoft/DialoGPT-medium`  
**Alokacja pamięci**:
- Elasticsearch: 2GB
- LLM Service: 4GB
- Debugging włączony
- Swagger włączony

### 3. `.env.production`
**Przeznaczenie**: Środowisko produkcyjne  
**RAM**: Agresywne użycie dla maksymalnej wydajności  
**Model LLM**: `microsoft/DialoGPT-large`  
**Alokacja pamięci**:
- Elasticsearch: 6GB
- LLM Service: 6GB
- SSL włączony
- Monitoring pełny

### 4. `.env.local`
**Przeznaczenie**: Lokalne testy, szybkie prototypowanie  
**RAM**: Minimalne użycie (~4GB całkowite)  
**Model LLM**: `microsoft/DialoGPT-small`  
**Alokacja pamięci**:
- Elasticsearch: 1GB
- LLM Service: 2GB
- Monitoring wyłączony

### 5. `.env.template`
**Przeznaczenie**: Template z wszystkimi dostępnymi opcjami  
**Użycie**: Kopiuj i dostosowuj do swoich potrzeb

## 🚀 Jak używać

### Szybki start (zalecane)
```bash
# Użyj domyślnej konfiguracji
cp deploy/.env deploy/.env.active
```

### Development
```bash
# Dla pracy developerskiej
cp deploy/.env.development deploy/.env.active
```

### Lokalne testy
```bash
# Dla szybkich testów z minimalnym użyciem RAM
cp deploy/.env.local deploy/.env.active
```

### Produkcja
```bash
# Dla środowiska produkcyjnego
cp deploy/.env.production deploy/.env.active
# UWAGA: Zmień hasła i tokeny przed użyciem!
```

## 📊 Porównanie konfiguracji

| Środowisko | Model LLM | ES RAM | LLM RAM | Całkowite | Zalecane dla |
|------------|-----------|---------|---------|-----------|--------------|
| **local** | DialoGPT-small | 1GB | 2GB | ~4GB | Szybkie testy |
| **development** | DialoGPT-medium | 2GB | 4GB | ~8GB | Codziennie dev |
| **default** | DialoGPT-medium | 3GB | 4GB | ~9GB | Standard 16GB |
| **production** | DialoGPT-large | 6GB | 6GB | ~14GB | Produkcja |

## 🔧 Dostosowywanie dla Twojego systemu

### Jeśli masz mniej niż 16GB RAM:
```bash
# Użyj konfiguracji lokalnej
cp deploy/.env.local deploy/.env.active

# Lub dostosuj domyślną:
# W pliku .env zmień:
ES_JAVA_OPTS=-Xms1g -Xmx2g
LLM_MAX_MEMORY=3g
LLM_MODEL_ID=microsoft/DialoGPT-small
```

### Jeśli masz więcej niż 16GB RAM:
```bash
# Użyj konfiguracji produkcyjnej
cp deploy/.env.production deploy/.env.active

# Lub zwiększ limity w .env:
ES_JAVA_OPTS=-Xms4g -Xmx8g
LLM_MAX_MEMORY=8g
LLM_MODEL_ID=microsoft/DialoGPT-large
```

## 🤖 Modele LLM - Wymagania pamięci

### microsoft/DialoGPT-small
- **RAM**: ~1-2GB
- **Zalecane dla**: Testy, prototypy
- **Jakość**: Podstawowa
- **Szybkość**: Bardzo szybka

### microsoft/DialoGPT-medium (domyślny)
- **RAM**: ~2-3GB
- **Zalecane dla**: Development, małe produkcje
- **Jakość**: Dobra
- **Szybkość**: Szybka

### microsoft/DialoGPT-large
- **RAM**: ~4-5GB
- **Zalecane dla**: Produkcja
- **Jakość**: Bardzo dobra
- **Szybkość**: Umiarkowana

### Alternatywne modele:
```bash
# Dla języka polskiego (eksperymentalne)
LLM_MODEL_ID=allegro/herbert-base-cased

# Dla wielojęzycznych konwersacji
LLM_MODEL_ID=facebook/blenderbot-1B-distill

# Dla szybkich odpowiedzi
LLM_MODEL_ID=facebook/blenderbot-400M-distill
```

## 🔐 Bezpieczeństwo

### Development (bezpieczeństwo podstawowe):
- Hasła proste (`dev123`, `local`)
- JWT secrets podstawowe
- SSL wyłączony

### Production (bezpieczeństwo pełne):
```bash
# Wygeneruj bezpieczne hasła:
ELASTIC_PASSWORD=$(openssl rand -base64 32)
GRAFANA_PASSWORD=$(openssl rand -base64 24)
JWT_SECRET=$(openssl rand -base64 32)

# Włącz SSL:
ENABLE_SSL=true
SSL_CERT_PATH=/path/to/your/cert.crt
SSL_KEY_PATH=/path/to/your/key.key
```

## 🚀 Uruchamianie z różnymi konfiguracjami

```bash
# Z domyślną konfiguracją
cd scripts
./rag-manager.sh start

# Z konkretną konfiguracją
cd deploy
cp .env.development .env
cd ../scripts
./rag-manager.sh start

# Sprawdzenie aktualnej konfiguracji
cd deploy
grep -E "LLM_MODEL_ID|ES_JAVA_OPTS|LLM_MAX_MEMORY" .env
```

## 🔍 Monitorowanie użycia pamięci

```bash
# Sprawdź użycie RAM przez kontenery
./rag-manager.sh monitor

# Lub bezpośrednio Docker:
docker stats es llm-service embedding-service
```

## ⚠️ Troubleshooting

### Problem: Out of Memory (OOM)
```bash
# Zmniejsz alokację pamięci:
ES_JAVA_OPTS=-Xms512m -Xmx1g
LLM_MAX_MEMORY=2g
LLM_MODEL_ID=microsoft/DialoGPT-small
```

### Problem: Wolne ładowanie modelu
```bash
# Sprawdź czy masz token HuggingFace:
HF_TOKEN=your_token_here

# Lub użyj mniejszego modelu:
LLM_MODEL_ID=microsoft/DialoGPT-small
```

### Problem: Model nie pobiera się
```bash
# Sprawdź miejsce na dysku:
df -h

# Sprawdź logi:
docker-compose logs llm-service
```

---

## 📝 Przykład użycia

```bash
# 1. Wybierz konfigurację
cp deploy/.env.development deploy/.env

# 2. Dostosuj do swoich potrzeb (opcjonalnie)
nano deploy/.env

# 3. Uruchom system
cd scripts
./rag-manager.sh setup

# 4. Sprawdź status
./rag-manager.sh status

# 5. Monitoruj
./rag-manager.sh monitor
```

**Gotowe! System RAG Suite będzie działał z optymalną konfiguracją dla Twojego środowiska.** 🎉
