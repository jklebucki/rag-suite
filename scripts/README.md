# RAG Suite Scripts

## 🐘 PostgreSQL Setup

### Opcja 1: Docker (Zalecane)
```bash
# Uruchom PostgreSQL w kontenerze Docker
./setup-postgres-docker.sh

# Zarządzaj kontenerem
./postgres-manager.sh status    # Sprawdź status
./postgres-manager.sh backup    # Utwórz backup
./postgres-manager.sh connect   # Połącz się z bazą
./postgres-manager.sh help      # Zobacz wszystkie opcje
```

### Opcja 2: Docker Compose
```bash
# Uruchom PostgreSQL z docker-compose
./setup-postgres-compose.sh

# Zarządzaj z docker-compose
docker-compose -f scripts/docker-compose.postgres.yml ps
docker-compose -f scripts/docker-compose.postgres.yml logs -f
```

### 🔧 Konfiguracja PostgreSQL:
- **Database:** rag-suite
- **Username:** postgres
- **Password:** postgres
- **Port:** 5432
- **Auto-restart:** Tak (po restarcie serwera)
- **Wolumen:** `/opt/rag-suite/postgresql` (Ubuntu best practices)

## � Security Tools

### JWT Key Generation
```bash
# Generuj bezpieczny klucz JWT
./generate-jwt-key.sh

# Dla konkretnego środowiska
./generate-jwt-key.sh dev
./generate-jwt-key.sh prod
```

## 🚀 LLM Integration

```bash
# 1. Przejdź do katalogu scripts
cd scripts

# 2. Uruchom pełną konfigurację środowiska
./rag-manager.sh setup

# 3. (Opcjonalnie) Wybierz konfigurację środowiska
./rag-manager.sh config

# 4. Sprawdź status
./rag-manager.sh status

# 5. Uruchom API (w nowym terminalu)
./rag-manager.sh build-api

# 6. Uruchom frontend (w nowym terminalu)
./rag-manager.sh build-ui

# 7. Przetestuj integrację
./rag-manager.sh test
```

## 📁 Dostępne konfiguracje środowiska

| Plik | RAM | Model LLM | Przeznaczenie |
|------|-----|-----------|---------------|
| `.env.local` | 4GB | DialoGPT-small | Szybkie testy |
| `.env.development` | 8GB | DialoGPT-medium | Development |
| `.env` | 9GB | DialoGPT-medium | Standard (16GB) |
| `.env.production` | 14GB | DialoGPT-large | Produkcja |

### Zmiana konfiguracji:
```bash
./rag-manager.sh config
```

## 📁 Dodane pliki

### Skrypty zarządzania
- `scripts/setup-llm.sh` - Automatyczna instalacja i konfiguracja LLM
- `scripts/test-llm-integration.sh` - Testy integracji między komponentami
- `scripts/rag-manager.sh` - Uniwersalny manager środowiska

### Konfiguracja Docker
- `deploy/docker-compose.yml` - Zaktualizowany z kontenerem LLM
- `deploy/docker-compose.prod.yml` - Konfiguracja produkcyjna
- `deploy/.env.template` - Template zmiennych środowiskowych

### Kod aplikacji
- `src/RAG.Orchestrator.Api/Features/Chat/ChatService.cs` - Zintegrowany z LLM
- `src/RAG.Orchestrator.Api/Features/Chat/ChatEndpoints.cs` - Dodane health checki
- `src/RAG.Orchestrator.Api/Extensions/ServiceCollectionExtensions.cs` - Rejestracja LLM service
- `src/RAG.Orchestrator.Api/appsettings.json` - Konfiguracja LLM

### Dokumentacja
- `docs/LLM-INTEGRATION.md` - Szczegółowa instrukcja integracji

## 🛠️ Dostępne komendy

```bash
./rag-manager.sh [KOMENDA]
```

### Podstawowe operacje
- `setup` - Pełna instalacja środowiska
- `start` - Start wszystkich serwisów
- `stop` - Stop wszystkich serwisów
- `restart` - Restart wszystkich serwisów
- `status` - Status serwisów i health checks

### Zarządzanie aplikacją
- `build-api` - Buduj i uruchom API
- `build-ui` - Buduj i uruchom frontend
- `config` - Zmień konfigurację środowiska

### Diagnostyka
- `test` - Uruchom testy integracji
- `logs` - Pokaż logi wszystkich serwisów
- `logs [service]` - Logi konkretnego serwisu
- `monitor` - Monitor zasobów w czasie rzeczywistym

### Maintenance
- `clean` - Wyczyść wszystkie kontenery i volumes

## 🔧 Konfiguracja

### 1. Zmienne środowiskowe
```bash
# Skopiuj template
cp deploy/.env.template deploy/.env

# Edytuj konfigurację
nano deploy/.env
```

### 2. Zmiana modelu LLM
Edytuj `.env`:
```bash
LLM_MODEL_ID=microsoft/DialoGPT-large
MAX_TOTAL_TOKENS=8192
```

### 3. Konfiguracja produkcyjna
```bash
# Użyj konfiguracji produkcyjnej
cd deploy
docker-compose -f docker-compose.prod.yml up -d
```

## 📊 Endpointy

### API Chat
- `GET /api/chat/sessions` - Lista sesji
- `POST /api/chat/sessions` - Nowa sesja
- `POST /api/chat/sessions/{id}/messages` - Wyślij wiadomość
- `GET /api/chat/health` - Status LLM

### Health Checks
- `GET /health` - Status API
- `http://localhost:9200/_cluster/health` - Elasticsearch
- `http://192.168.21.14:8580/health` - Embedding Service
- `http://localhost:8581/health` - LLM Service

## 🌐 Dostęp do usług

| Serwis | URL | Dane logowania |
|--------|-----|----------------|
| Orchestrator API | http://localhost:7107 | - |
| Swagger UI | http://localhost:7107 | - |
| Web UI | http://localhost:5173 | - |
| Elasticsearch | http://localhost:9200 | elastic / elastic |
| Kibana | http://localhost:5601 | elastic / elastic |
| LLM Service | http://localhost:8581 | - |
| Embedding Service | http://192.168.21.14:8580 | - |

## 🧪 Testowanie

### Test podstawowy
```bash
# Sprawdź wszystkie serwisy
curl http://localhost:7107/health

# Test LLM bezpośrednio
curl -X POST http://localhost:8581/generate \
  -H "Content-Type: application/json" \
  -d '{"inputs": "Cześć!", "parameters": {"max_new_tokens": 50}}'
```

### Test chat API
```bash
# Utwórz sesję
SESSION_ID=$(curl -s -X POST http://localhost:7107/api/chat/sessions \
  -H "Content-Type: application/json" \
  -d '{"title": "Test"}' | jq -r '.data.id')

# Wyślij wiadomość
curl -X POST "http://localhost:7107/api/chat/sessions/$SESSION_ID/messages" \
  -H "Content-Type: application/json" \
  -d '{"message": "Jak działają bazy danych Oracle?"}'
```

## 🔍 Troubleshooting

### LLM Service nie startuje
```bash
# Sprawdź logi
docker-compose logs llm-service

# Sprawdź miejsce na dysku
df -h

# Restart z czasem na pobranie modelu
docker-compose restart llm-service
```

### Brak pamięci
```bash
# Zwiększ limity Docker Desktop do 8GB+
# Lub użyj mniejszego modelu w .env:
LLM_MODEL_ID=microsoft/DialoGPT-small
```

### API nie łączy się z LLM
```bash
# Sprawdź status wszystkich serwisów
./rag-manager.sh status

# Sprawdź health check LLM
curl http://localhost:8581/health
```

## 🔄 Integracja z frontendem

Przykład JavaScript/TypeScript:
```typescript
// Połączenie z chat API
const sendMessage = async (sessionId: string, message: string) => {
  const response = await fetch(`/api/chat/sessions/${sessionId}/messages`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ message })
  });
  return await response.json();
};

// Sprawdzenie statusu LLM
const checkLlmHealth = async () => {
  const response = await fetch('/api/chat/health');
  return await response.json();
};
```

## 📈 Monitoring

### Logs
```bash
# Wszystkie logi
./rag-manager.sh logs

# Konkretny serwis
./rag-manager.sh logs llm-service

# Monitor w czasie rzeczywistym
./rag-manager.sh monitor
```

### Metryki
- Kibana Dashboard: http://localhost:5601
- Resource Monitor: `./rag-manager.sh monitor`
- Docker Stats: `docker stats`

## 🚀 Deployment na produkcję

1. **Użyj docker-compose.prod.yml**:
```bash
cd deploy
docker-compose -f docker-compose.prod.yml up -d
```

2. **Konfiguruj bezpieczeństwo**:
- Zmień hasła w `.env`
- Włącz HTTPS
- Skonfiguruj firewall

3. **Skaaluj zasoby**:
- Zwiększ pamięć dla Elasticsearch
- Użyj większego modelu LLM
- Dodaj load balancer

---

## ✅ Co zostało dodane

✅ Kontener LLM (Text Generation Inference)  
✅ Integracja z ChatService  
✅ Automatyczne skrypty instalacji  
✅ Testy integracji  
✅ Health checks  
✅ Monitoring  
✅ Dokumentacja  
✅ Konfiguracja produkcyjna  
✅ Manager środowiska  

**Projekt RAG Suite jest teraz w pełni zintegrowany z LLM! 🎉**
# Document processing containers

Deployment scripts for the CPU and NVIDIA GPU variants are in [document-processing](document-processing/). See the [deployment guide](../deploy/document-processing/README.md) before using them.
