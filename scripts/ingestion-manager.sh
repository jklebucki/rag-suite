#!/bin/bash

# RAG Ingestion Management Script

PROJECT_ROOT="/Users/jklebucki/Projects/rag-suite"
INGESTION_PROJECT="$PROJECT_ROOT/src/RAG.Ingestion.Worker"
DOCUMENTS_PATH="$PROJECT_ROOT/data/documents"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "🔍 RAG Document Ingestion Management"
echo "===================================="

show_help() {
    echo "Usage: $0 {build|run|test|add-documents|check-es|clean}"
    echo ""
    echo "Commands:"
    echo "  build         - Build the ingestion worker"
    echo "  run           - Run the ingestion worker"
    echo "  test          - Test Elasticsearch connection"
    echo "  add-documents - Add sample documents to process"
    echo "  check-es      - Check Elasticsearch status"
    echo "  clean         - Clean up build artifacts"
}

build_project() {
    echo -e "${YELLOW}🔨 Building RAG.Ingestion.Worker...${NC}"
    cd "$INGESTION_PROJECT"
    dotnet build
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ Build successful${NC}"
    else
        echo -e "${RED}❌ Build failed${NC}"
        exit 1
    fi
}

run_worker() {
    echo -e "${YELLOW}🚀 Starting RAG Ingestion Worker...${NC}"
    cd "$INGESTION_PROJECT"
    dotnet run
}

test_elasticsearch() {
    echo -e "${YELLOW}🔍 Testing Elasticsearch connection...${NC}"
    
    response=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:9200)
    
    if [ "$response" = "200" ]; then
        echo -e "${GREEN}✅ Elasticsearch is running${NC}"
        
        # Get cluster info
        echo -e "${YELLOW}Cluster Info:${NC}"
        curl -s "http://localhost:9200" | jq '.name, .cluster_name, .version.number'
        
        # Check indices
        echo -e "${YELLOW}Indices:${NC}"
        curl -s "http://localhost:9200/_cat/indices?v"
    else
        echo -e "${RED}❌ Elasticsearch is not accessible${NC}"
        echo "Make sure Elasticsearch is running on http://localhost:9200"
    fi
}

add_sample_documents() {
    echo -e "${YELLOW}📄 Adding sample documents...${NC}"
    
    # Create sample PDF content (text file for now, you can convert to PDF later)
    cat > "$DOCUMENTS_PATH/company-policies.txt" << 'EOF'
# Polityki Firmowe - Przykład

## Polityka Bezpieczeństwa IT

### 1. Hasła
- Minimalna długość: 12 znaków
- Wymagane: wielkie i małe litery, cyfry, znaki specjalne
- Zmiana co 90 dni
- Zakaz używania poprzednich 12 haseł

### 2. Dostęp do Systemów
- Uwierzytelnianie dwuskładnikowe obowiązkowe
- Dostęp na zasadzie najmniejszych uprawnień
- Regularne audyty dostępu

### 3. Bezpieczeństwo Danych
- Szyfrowanie danych w ruchu i spoczynku
- Klasyfikacja danych (publiczne, wewnętrzne, poufne, tajne)
- Backup codziennie, test odtwarzania miesięcznie

## Polityka Pracy Zdalnej

### Wymagania Techniczne
- VPN do połączenia z siecią firmową
- Antywirus aktualny na urządzeniu
- Automatyczne blokowanie ekranu po 10 min

### Bezpieczeństwo
- Zakaz pracy w miejscach publicznych z danymi poufnymi
- Używanie słuchawek podczas rozmów biznesowych
- Zabezpieczenie fizyczne urządzenia

EOF

    cat > "$DOCUMENTS_PATH/api-documentation.md" << 'EOF'
# API Documentation - RAG Orchestrator

## Overview
RAG Orchestrator API provides endpoints for chat interactions and document search.

## Authentication
All requests require API key in header:
```
Authorization: Bearer YOUR_API_KEY
```

## Endpoints

### Chat API

#### POST /api/chat/sessions
Create new chat session
```json
{
  "userId": "string",
  "title": "string"
}
```

#### POST /api/chat/sessions/{sessionId}/messages
Send message to chat
```json
{
  "message": "string",
  "useRag": true
}
```

#### GET /api/chat/sessions/{sessionId}
Get chat session details

### Search API

#### GET /api/search
Search documents
```
GET /api/search?query=elasticsearch&limit=10
```

#### POST /api/search
Advanced search
```json
{
  "query": "string",
  "filters": {},
  "limit": 10,
  "useVector": true
}
```

## Response Format
```json
{
  "success": true,
  "data": {},
  "error": null,
  "timestamp": "2024-08-24T10:00:00Z"
}
```

## Error Codes
- 400: Bad Request
- 401: Unauthorized
- 404: Not Found
- 500: Internal Server Error
EOF

    echo -e "${GREEN}✅ Sample documents created in $DOCUMENTS_PATH${NC}"
    echo "Documents:"
    ls -la "$DOCUMENTS_PATH"
}

check_elasticsearch_status() {
    echo -e "${YELLOW}📊 Elasticsearch Status Check...${NC}"
    
    # Check if ES is running
    if curl -s "http://localhost:9200" > /dev/null; then
        echo -e "${GREEN}✅ Elasticsearch is running${NC}"
        
        # Check cluster health
        echo -e "${YELLOW}Cluster Health:${NC}"
        curl -s "http://localhost:9200/_cluster/health?pretty"
        
        # Check RAG indices
        echo -e "${YELLOW}RAG Indices:${NC}"
        curl -s "http://localhost:9200/_cat/indices/rag_*?v"
        
        # Check document count
        echo -e "${YELLOW}Document Count:${NC}"
        curl -s "http://localhost:9200/rag_documents/_count?pretty" 2>/dev/null || echo "Index not found"
        
    else
        echo -e "${RED}❌ Elasticsearch is not running${NC}"
    fi
}

clean_project() {
    echo -e "${YELLOW}🧹 Cleaning build artifacts...${NC}"
    cd "$INGESTION_PROJECT"
    dotnet clean
    echo -e "${GREEN}✅ Clean completed${NC}"
}

# Main script logic
case "$1" in
    build)
        build_project
        ;;
    run)
        build_project
        run_worker
        ;;
    test)
        test_elasticsearch
        ;;
    add-documents)
        add_sample_documents
        ;;
    check-es)
        check_elasticsearch_status
        ;;
    clean)
        clean_project
        ;;
    *)
        show_help
        exit 1
        ;;
esac
