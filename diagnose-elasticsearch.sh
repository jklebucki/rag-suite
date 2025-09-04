#!/bin/bash

# RAG Suite - Elasticsearch Debug Script
# Skrypt do diagnozy problemów z rekonstrukcją dokumentów

set -e

# Konfiguracja
ES_HOST="http://localhost:9200"
ES_USER="elastic"
ES_PASS="elastic"
ES_INDEX="rag-chunks"

echo "==================================================="
echo "   RAG Suite - Elasticsearch Diagnostic Tool"
echo "==================================================="

# Funkcja do wykonywania zapytań do Elasticsearch
query_es() {
    local query="$1"
    local description="$2"
    
    echo "[INFO] $description"
    echo "Query: $query"
    
    curl -s -u "$ES_USER:$ES_PASS" \
        -H "Content-Type: application/json" \
        -X POST "$ES_HOST/$ES_INDEX/_search" \
        -d "$query" | jq -r '
        .hits.total.value as $total |
        .took as $took |
        "Total hits: \($total), took: \($took)ms" |
        .,' |
    
    curl -s -u "$ES_USER:$ES_PASS" \
        -H "Content-Type: application/json" \
        -X POST "$ES_HOST/$ES_INDEX/_search" \
        -d "$query" | jq -r '
        .hits.hits[] | 
        "Chunk \(._source.position.chunkIndex // "N/A"): Content length: \(._source.content | length) chars, TotalChunks: \(._source.position.totalChunks // "N/A")"' | head -5
    
    echo ""
}

# Test 1: Sprawdź ogólny stan indeksu
echo "[TEST 1] Index Overview"
curl -s -u "$ES_USER:$ES_PASS" "$ES_HOST/$ES_INDEX/_stats" | jq -r '
.indices."rag-chunks".total.docs.count as $count |
.indices."rag-chunks".total.store.size_in_bytes as $size |
"Documents: \($count), Size: \($size) bytes"'
echo ""

# Test 2: Sprawdź sample dokumentów
echo "[TEST 2] Sample Documents"
query_es '{
  "size": 3,
  "_source": ["sourceFile", "content", "position"],
  "query": {"match_all": {}}
}' "Getting sample documents"

# Test 3: Sprawdź konkretny dokument z logów
TEST_FILE="X:\\Citronex\\IFS Materialy\\Instrukcje\\Notatka ze spotkania - WinSped - 20240422.docx"
echo "[TEST 3] Specific Document Test"
echo "Testing file: $TEST_FILE"

# Test 3a: Zapytanie z .keyword
query_es "{
  \"size\": 5,
  \"_source\": [\"sourceFile\", \"content\", \"position\"],
  \"query\": {
    \"term\": {
      \"sourceFile.keyword\": \"$TEST_FILE\"
    }
  },
  \"sort\": [{\"position.chunkIndex\": {\"order\": \"asc\"}}]
}" "Query with .keyword field"

# Test 3b: Zapytanie bez .keyword
query_es "{
  \"size\": 5,
  \"_source\": [\"sourceFile\", \"content\", \"position\"],
  \"query\": {
    \"term\": {
      \"sourceFile\": \"$TEST_FILE\"
    }
  },
  \"sort\": [{\"position.chunkIndex\": {\"order\": \"asc\"}}]
}" "Query without .keyword field"

# Test 3c: Zapytanie z match
query_es "{
  \"size\": 5,
  \"_source\": [\"sourceFile\", \"content\", \"position\"],
  \"query\": {
    \"match\": {
      \"sourceFile\": \"$TEST_FILE\"
    }
  },
  \"sort\": [{\"position.chunkIndex\": {\"order\": \"asc\"}}]
}" "Query with match field"

# Test 4: Sprawdź mapowanie indeksu
echo "[TEST 4] Index Mapping"
curl -s -u "$ES_USER:$ES_PASS" "$ES_HOST/$ES_INDEX/_mapping" | jq -r '
.["rag-chunks"].mappings.properties.sourceFile // "sourceFile mapping not found",
.["rag-chunks"].mappings.properties.content // "content mapping not found",
.["rag-chunks"].mappings.properties.position // "position mapping not found"'
echo ""

# Test 5: Sprawdź unikalne pliki źródłowe
echo "[TEST 5] Unique Source Files (first 10)"
curl -s -u "$ES_USER:$ES_PASS" \
    -H "Content-Type: application/json" \
    -X POST "$ES_HOST/$ES_INDEX/_search" \
    -d '{
      "size": 0,
      "aggs": {
        "unique_files": {
          "terms": {
            "field": "sourceFile.keyword",
            "size": 10
          }
        }
      }
    }' | jq -r '.aggregations.unique_files.buckets[] | "\(.key): \(.doc_count) chunks"'

echo ""
echo "==================================================="
echo "           Diagnostic Complete"
echo "==================================================="
