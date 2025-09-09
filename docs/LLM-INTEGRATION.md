# RAG Suite - Instrukcja integracji LLM

## Przegląd

Ten dokument opisuje jak zintegrować kontener LLM z projektem RAG Suite w celu obsługi wewnętrznego chat-a AI.

## Architektura

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   RAG.Web.UI    │────│ RAG.Orchestrator│────│   Elasticsearch │
│   (Frontend)    │    │      .Api       │    │   (Wyszukiwanie)│
└─────────────────┘    └─────────────────┘    └─────────────────┘
                              │
                              ├──────────────────────────────────┐
                              │                                  │
                    ┌─────────────────┐              ┌─────────────────┐
                    │ Embedding Service│              │   LLM Service   │
                    │   (Embeddings)  │              │ (Generowanie)   │
                    └─────────────────┘              └─────────────────┘
```

## Komponenty

### 1. Embedding Service (Text Embeddings Inference)
- **Kontener**: `ghcr.io/huggingface/text-embeddings-inference:cpu-1.8`
- **Port**: 8580
- **Model**: intfloat/multilingual-e5-base
- **Funkcja**: Tworzenie embeddingów do wyszukiwania semantycznego

### 2. LLM Service (Text Generation Inference)
- **Kontener**: `ghcr.io/huggingface/text-generation-inference:2.2.0`
- **Port**: 8581
- **Model**: microsoft/DialoGPT-medium (konfigurowalny)
- **Funkcja**: Generowanie odpowiedzi AI

### 3. Elasticsearch
- **Port**: 9200
- **Funkcja**: Przechowywanie dokumentów i wyszukiwanie RAG

### 4. RAG.Orchestrator.Api
- **Port**: 7107
- **Funkcja**: Orchestracja zapytań, integracja z LLM i Elasticsearch

## Instalacja i konfiguracja

### Krok 1: Uruchomienie skryptu instalacyjnego

```bash
cd scripts
./setup-llm.sh
```

Skrypt automatycznie:
- Sprawdzi wymagania systemowe
- Pobierze obrazy Docker
- Utworzy niezbędne katalogi
- Skonfiguruje zmienne środowiskowe
- Uruchomi wszystkie serwisy

### Krok 2: Konfiguracja środowiska

Edytuj plik `deploy/.env`:

```bash
# Hugging Face Token (opcjonalny, dla modeli gated)
HF_TOKEN=your_huggingface_token_here

# Zmiana modelu LLM (opcjonalnie)
LLM_MODEL_ID=microsoft/DialoGPT-medium

# Parametry generowania
MAX_TOTAL_TOKENS=4096
MAX_INPUT_LENGTH=3072
```

### Krok 3: Weryfikacja instalacji

```bash
cd scripts
./test-llm-integration.sh
```

## Konfiguracja aplikacji

### appsettings.json

Aplikacja została skonfigurowana z następującymi ustawieniami:

```json
{
  "Services": {
    "Elasticsearch": {
      "Url": "http://localhost:9200",
      "Username": "elastic",
      "Password": "elastic"
    },
    "EmbeddingService": {
      "Url": "http://192.168.21.14:8580"
    },
    "LlmService": {
      "Url": "http://localhost:8581",
      "MaxTokens": 4096,
      "Temperature": 0.7,
      "TopP": 0.9
    }
  },
  "Chat": {
    "MaxMessageLength": 2000,
    "MaxMessagesPerSession": 100,
    "SessionTimeoutMinutes": 60
  }
}
```

## API Endpoints

### Chat Management
- `GET /api/chat/sessions` - Lista sesji chat
- `POST /api/chat/sessions` - Utworzenie nowej sesji
- `GET /api/chat/sessions/{id}` - Pobranie sesji
- `POST /api/chat/sessions/{id}/messages` - Wysłanie wiadomości
- `DELETE /api/chat/sessions/{id}` - Usunięcie sesji

### Health Monitoring
- `GET /health` - Status API
- `GET /api/chat/health` - Status serwisu LLM

## Uruchomienie aplikacji

### 1. Start infrastruktury
```bash
cd deploy
docker-compose up -d
```

### 2. Uruchomienie API
```bash
cd src/RAG.Orchestrator.Api
dotnet run
```

### 3. Uruchomienie Frontend (opcjonalnie)
```bash
cd src/RAG.Web.UI
npm install
npm run dev
```

## Monitorowanie

### 1. Sprawdzanie statusu kontenerów
```bash
docker ps
docker-compose logs -f llm-service
```

### 2. Kibana Dashboard
- URL: http://localhost:5601
- Dane logowania: elastic / elastic

### 3. API Health Checks
```bash
# Sprawdzenie ogólnego statusu
curl http://localhost:7107/health

# Sprawdzenie statusu LLM
curl http://localhost:7107/api/chat/health

# Sprawdzenie bezpośrednio LLM service
curl http://localhost:8581/health
```

## Testowanie funkcjonalności

### Przykład użycia API

```bash
# 1. Utworzenie sesji chat
session_id=$(curl -s -X POST http://localhost:7107/api/chat/sessions \
  -H "Content-Type: application/json" \
  -d '{"title": "Test Chat"}' | jq -r '.data.id')

# 2. Wysłanie wiadomości
curl -X POST "http://localhost:7107/api/chat/sessions/$session_id/messages" \
  -H "Content-Type: application/json" \
  -d '{"message": "Cześć! Jak działają bazy danych Oracle?"}'
```

## Rozwiązywanie problemów

### Problem: LLM Service nie odpowiada
**Rozwiązanie**:
```bash
# Sprawdź logi
docker-compose logs llm-service

# Restart serwisu
docker-compose restart llm-service
```

### Problem: Brak pamięci
**Rozwiązanie**:
- Zwiększ pamięć Docker Desktop (minimum 8GB)
- Użyj mniejszego modelu LLM
- Zmień `ES_JAVA_OPTS=-Xms512m -Xmx512m`

### Problem: Model nie pobiera się
**Rozwiązanie**:
```bash
# Sprawdź przestrzeń dyskową
df -h

# Sprawdź połączenie internetowe
docker-compose logs llm-service | grep -i download
```

## Dostosowywanie

### Zmiana modelu LLM

1. Edytuj `docker-compose.yml`:
```yaml
llm-service:
  environment:
    - MODEL_ID=your-preferred-model
  command:
    - --model-id
    - "your-preferred-model"
```

2. Restart serwisu:
```bash
docker-compose down llm-service
docker-compose up -d llm-service
```

### Dodanie autoryzacji

Edytuj `ChatService.cs` aby dodać uwierzytelnianie:
```csharp
// Dodaj middleware autoryzacji w Program.cs
app.UseAuthentication();
app.UseAuthorization();
```

## Integracja z RAG.Web.UI

Frontend może komunikować się z API przez standardowe HTTP requests:

```javascript
// Przykład w React/TypeScript
const sendMessage = async (sessionId: string, message: string) => {
  const response = await fetch(`/api/chat/sessions/${sessionId}/messages`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ message })
  });
  return await response.json();
};
```

## Bezpieczeństwo

### Podstawowe zabezpieczenia
- Zmień domyślne hasło Elasticsearch
- Dodaj HTTPS w środowisku produkcyjnym
- Skonfiguruj CORS dla specyficznych domen
- Implementuj rate limiting

### Konfiguracja produkcyjna
```yaml
# W docker-compose.yml dla produkcji
environment:
  - ELASTIC_PASSWORD=${ELASTIC_PASSWORD}
  - TLS_ENABLED=true
```

## Wsparcie

W przypadku problemów:
1. Sprawdź logi Docker: `docker-compose logs`
2. Uruchom skrypt testowy: `./scripts/test-llm-integration.sh`
3. Sprawdź status wszystkich serwisów: `docker ps`
4. Zweryfikuj konfigurację w `.env` i `appsettings.json`

---

## Struktura plików po integracji

```
rag-suite/
├── deploy/
│   ├── docker-compose.yml          # ✅ Zaktualizowany z LLM
│   ├── .env                        # ✅ Nowy - konfiguracja środowiska
│   └── elastic/                    # Bez zmian
├── scripts/
│   ├── setup-llm.sh               # ✅ Nowy - instalacja LLM
│   └── test-llm-integration.sh     # ✅ Nowy - testy integracji
├── src/RAG.Orchestrator.Api/
│   ├── appsettings.json            # ✅ Zaktualizowany
│   ├── Features/Chat/
│   │   ├── ChatService.cs          # ✅ Zaktualizowany z LLM
│   │   └── ChatEndpoints.cs        # ✅ Zaktualizowany
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs # ✅ Zaktualizowany
└── docs/
    └── LLM-INTEGRATION.md          # ✅ Ten dokument
```
