#!/bin/bash

# RAG Suite - Load Sample Data Script
# This script loads sample documents about RAG systems into Elasticsearch

set -e

# Configuration
ES_HOST="http://localhost:9200"
ES_USER="elastic"
ES_PASS="elastic"
ES_INDEX="rag_knowledge_base"

echo "==================================================="
echo "       RAG Suite - Loading Sample Data"
echo "==================================================="

# Create index with mapping for RAG documents
echo "[INFO] Creating Elasticsearch index: $ES_INDEX"

curl -s -u $ES_USER:$ES_PASS -X PUT "$ES_HOST/$ES_INDEX" \
  -H "Content-Type: application/json" \
  -d '{
    "mappings": {
      "properties": {
        "title": {
          "type": "text",
          "analyzer": "standard"
        },
        "content": {
          "type": "text",
          "analyzer": "standard"
        },
        "category": {
          "type": "keyword"
        },
        "tags": {
          "type": "keyword"
        },
        "created_at": {
          "type": "date"
        },
        "embedding": {
          "type": "dense_vector",
          "dims": 384
        }
      }
    },
    "settings": {
      "number_of_shards": 1,
      "number_of_replicas": 0
    }
  }' | jq '.'

# Sample documents about RAG systems
echo "[INFO] Loading sample documents..."

# Document 1: RAG Introduction
curl -s -u $ES_USER:$ES_PASS -X POST "$ES_HOST/$ES_INDEX/_doc" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Wprowadzenie do RAG (Retrieval Augmented Generation)",
    "content": "RAG (Retrieval Augmented Generation) to technika łącząca wyszukiwanie informacji z generowaniem tekstu przez modele językowe. System RAG składa się z dwóch głównych komponentów: retriever (mechanizm wyszukiwania) oraz generator (model językowy). Retriever wyszukuje relevantne dokumenty z bazy wiedzy, a następnie generator wykorzystuje te informacje do utworzenia odpowiedzi. Główne zalety RAG to: aktualne informacje, zmniejszenie halucynacji modeli językowych, możliwość dodawania nowej wiedzy bez przekształcania modelu.",
    "category": "technology",
    "tags": ["RAG", "AI", "NLP", "retrieval", "generation"],
    "created_at": "2025-08-24T19:00:00Z"
  }' | jq '.'

# Document 2: RAG Architecture
curl -s -u $ES_USER:$ES_PASS -X POST "$ES_HOST/$ES_INDEX/_doc" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Architektura systemu RAG",
    "content": "Typowa architektura RAG obejmuje następujące komponenty: 1) Vector Store - baza danych wektorowych przechowująca embeddingi dokumentów, 2) Embedding Model - model konwertujący teksty na reprezentacje wektorowe, 3) Retriever - mechanizm wyszukiwania podobnych dokumentów, 4) LLM (Large Language Model) - model generujący odpowiedzi, 5) Orchestrator - komponent zarządzający przepływem danych. Proces działa następująco: zapytanie użytkownika jest konwertowane na embedding, wyszukiwane są podobne dokumenty, kontekst jest przekazywany do LLM wraz z zapytaniem, model generuje odpowiedź bazując na dostarczonym kontekście.",
    "category": "architecture",
    "tags": ["RAG", "architecture", "vector-store", "embedding", "LLM"],
    "created_at": "2025-08-24T19:00:00Z"
  }' | jq '.'

# Document 3: Elasticsearch in RAG
curl -s -u $ES_USER:$ES_PASS -X POST "$ES_HOST/$ES_INDEX/_doc" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Elasticsearch jako Vector Store w RAG",
    "content": "Elasticsearch może służyć jako vector store w systemach RAG dzięki wsparciu dla dense_vector field type. Umożliwia to przechowywanie embeddingów dokumentów i wykonywanie wyszukiwania semantycznego. Kluczowe funkcje: kNN (k-nearest neighbors) search, hybrid search łączący wyszukiwanie tekstowe z wektorowym, skalowalność i wydajność, integracja z narzędziami analitycznymi. Elasticsearch oferuje też funkcje takie jak filtering, agregacje i real-time indexing, co czyni go idealnym wyborem dla produkcyjnych systemów RAG.",
    "category": "technology",
    "tags": ["Elasticsearch", "vector-store", "kNN", "search", "RAG"],
    "created_at": "2025-08-24T19:00:00Z"
  }' | jq '.'

# Document 4: LLM Models in RAG
curl -s -u $ES_USER:$ES_PASS -X POST "$ES_HOST/$ES_INDEX/_doc" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Modele językowe w systemach RAG",
    "content": "W systemach RAG można używać różnych modeli językowych: GPT-4, Claude, Llama, Mistral, czy mniejsze modele jak GPT-2 dla testów. Wybór modelu zależy od wymagań dotyczących jakości odpowiedzi, kosztów i zasobów obliczeniowych. Większe modele jak GPT-4 generują lepsze odpowiedzi ale wymagają więcej zasobów. Mniejsze modele jak GPT-2 są szybsze i tańsze, ale mogą generować krótsze lub mniej dokładne odpowiedzi. Kluczowe parametry: max_tokens (długość odpowiedzi), temperature (kreatywność), top_p (różnorodność wyników).",
    "category": "technology",
    "tags": ["LLM", "GPT", "Claude", "Llama", "language-models", "RAG"],
    "created_at": "2025-08-24T19:00:00Z"
  }' | jq '.'

# Document 5: RAG Best Practices
curl -s -u $ES_USER:$ES_PASS -X POST "$ES_HOST/$ES_INDEX/_doc" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Najlepsze praktyki dla systemów RAG",
    "content": "Aby zbudować skuteczny system RAG, warto stosować następujące praktyki: 1) Jakość danych - czyste, strukturyzowane dokumenty w bazie wiedzy, 2) Chunking - podział długich dokumentów na mniejsze fragmenty, 3) Embedding quality - użycie odpowiednich modeli embeddingów, 4) Prompt engineering - tworzenie skutecznych promptów dla LLM, 5) Evaluation - regularne testowanie jakości odpowiedzi, 6) Monitoring - śledzenie wydajności i dokładności systemu, 7) User feedback - zbieranie opinii użytkowników do ulepszania systemu.",
    "category": "best-practices",
    "tags": ["RAG", "best-practices", "chunking", "evaluation", "monitoring"],
    "created_at": "2025-08-24T19:00:00Z"
  }' | jq '.'

# Wait for indexing
echo "[INFO] Waiting for documents to be indexed..."
sleep 2

# Verify data loading
echo "[INFO] Verifying loaded data..."
DOC_COUNT=$(curl -s -u $ES_USER:$ES_PASS "$ES_HOST/$ES_INDEX/_count" | jq '.count')
echo "[SUCCESS] Loaded $DOC_COUNT documents into $ES_INDEX"

# Show sample search
echo "[INFO] Testing search functionality..."
curl -s -u $ES_USER:$ES_PASS -X POST "$ES_HOST/$ES_INDEX/_search" \
  -H "Content-Type: application/json" \
  -d '{
    "query": {
      "multi_match": {
        "query": "RAG system",
        "fields": ["title^2", "content"]
      }
    },
    "size": 3
  }' | jq '.hits.hits[] | {title: ._source.title, score: ._score}'

echo "==================================================="
echo "        Sample Data Loading Complete!"
echo "==================================================="
echo ""
echo "📊 Loaded sample documents about:"
echo "  • RAG Introduction and concepts"
echo "  • RAG Architecture and components"  
echo "  • Elasticsearch as Vector Store"
echo "  • LLM Models in RAG systems"
echo "  • RAG Best Practices"
echo ""
echo "🔍 You can now test the chat with questions like:"
echo "  • 'What is RAG?'"
echo "  • 'How does RAG architecture work?'"
echo "  • 'What are RAG best practices?'"
echo "  • 'How to use Elasticsearch in RAG?'"
echo ""
