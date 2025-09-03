#!/bin/bash

# Skrypt do pełnej reinstalacji wszystkich serwisów RAG-Suite
# Autor: RAG-Suite
# Data: 2025-09-03

set -e

echo "=== RAG-Suite: Pełna reinstalacja serwisów ==="
echo "Data: $(date)"
echo ""

# Przejdź do katalogu ze skryptem
cd "$(dirname "$0")"

echo "1. Zatrzymywanie wszystkich serwisów..."
docker compose -f compose.yml down

echo "2. Usuwanie kontenerów..."
docker compose -f compose.yml rm -f

echo "3. Pobieranie najnowszych obrazów..."
docker compose -f compose.yml pull

echo "4. Uruchamianie wszystkich serwisów..."
docker compose -f compose.yml up -d

echo "5. Sprawdzanie statusu serwisów..."
sleep 15

# Sprawdź Elasticsearch
echo "Sprawdzanie Elasticsearch..."
if curl -f -u elastic:elastic http://localhost:9200/_cluster/health >/dev/null 2>&1; then
    echo "✅ Elasticsearch działa poprawnie"
else
    echo "❌ Elasticsearch nie odpowiada"
fi

# Sprawdź embedding service
echo "Sprawdzanie serwisu embedding..."
if curl -f http://localhost:8580/health >/dev/null 2>&1; then
    echo "✅ Serwis embedding działa poprawnie"
    echo "Nowa konfiguracja:"
    curl -s http://localhost:8580/info | jq '.max_input_length, .auto_truncate'
else
    echo "❌ Serwis embedding nie odpowiada"
fi

echo ""
echo "✅ Reinstalacja zakończona!"
echo "📋 Sprawdź logi w razie problemów:"
echo "   docker logs es-srv"
echo "   docker logs embedding-service-srv"
