#!/bin/bash

# Skrypt do reinstalacji serwisu embedding z nową konfiguracją
# Autor: RAG-Suite
# Data: 2025-09-03

set -e

echo "=== RAG-Suite: Reinstalacja serwisu embedding ==="
echo "Data: $(date)"
echo ""

# Przejdź do katalogu ze skryptem
cd "$(dirname "$0")"

echo "1. Zatrzymywanie serwisu embedding..."
docker-compose -f compose.yml stop embedding-service

echo "2. Usuwanie kontenera embedding..."
docker-compose -f compose.yml rm -f embedding-service

echo "3. Pobieranie najnowszego obrazu..."
docker-compose -f compose.yml pull embedding-service

echo "4. Uruchamianie serwisu embedding z nową konfiguracją..."
docker-compose -f compose.yml up -d embedding-service

echo "5. Sprawdzanie statusu serwisu..."
sleep 10

# Sprawdź czy serwis jest dostępny
if curl -f http://localhost:8580/health >/dev/null 2>&1; then
    echo "✅ Serwis embedding działa poprawnie"
else
    echo "❌ Serwis embedding nie odpowiada"
    echo "Sprawdź logi: docker logs embedding-service-srv"
    exit 1
fi

echo "6. Sprawdzanie nowej konfiguracji..."
echo "Aktualna konfiguracja serwisu:"
curl -s http://localhost:8580/info | jq '.'

echo ""
echo "✅ Reinstalacja zakończona pomyślnie!"
echo "📋 Nowe parametry:"
echo "   - max_input_length: 512 tokenów"
echo "   - auto_truncate: włączone"
echo ""
echo "Możesz teraz uruchomić RAG.Collector z większymi chunkami."
