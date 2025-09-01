#!/bin/bash

# RAG Suite Deployment Script
# Aktualizuje kod z git, buduje aplikację .NET i React, wdraża na serwer

set -e

# Kolory dla outputu
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Zmienne konfiguracyjne
APP_NAME="rag-suite"
APP_USER="www-data"
APP_DIR="/var/www/rag-suite"
GIT_BRANCH="${1:-main}"

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}    RAG Suite Deployment${NC}"
echo -e "${BLUE}========================================${NC}"
echo -e "${YELLOW}Branch: ${GIT_BRANCH}${NC}"
echo -e "${YELLOW}Katalog: ${APP_DIR}${NC}"
echo ""

# Sprawdź czy skrypt jest uruchamiany jako root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}Ten skrypt musi być uruchomiony jako root (użyj sudo)${NC}"
   exit 1
fi

# Sprawdź czy katalog aplikacji istnieje
if [ ! -d "$APP_DIR" ]; then
    echo -e "${RED}Katalog aplikacji nie istnieje: $APP_DIR${NC}"
    echo -e "${YELLOW}Uruchom najpierw: production-setup.sh${NC}"
    exit 1
fi

echo -e "${BLUE}[1/8] Zatrzymywanie serwisów...${NC}"

# Zatrzymaj serwis RAG API
if systemctl is-active --quiet rag-api; then
    echo -e "${YELLOW}Zatrzymywanie RAG API...${NC}"
    systemctl stop rag-api
    sleep 2
fi

echo -e "${BLUE}[2/8] Aktualizacja kodu z Git...${NC}"

cd $APP_DIR

# Sprawdź czy są uncommitted changes
if [ -n "$(git status --porcelain)" ]; then
    echo -e "${YELLOW}⚠ Wykryto niezapisane zmiany, wykonywanie stash...${NC}"
    git stash
fi

# Aktualizuj kod
git fetch origin
git checkout $GIT_BRANCH
git pull origin $GIT_BRANCH

echo -e "${BLUE}[3/8] Przygotowanie katalogów build...${NC}"

# Usuń stary build
if [ -d "build" ]; then
    rm -rf build
fi

# Utwórz strukturę build
mkdir -p build/api
mkdir -p build/web

echo -e "${BLUE}[4/8] Budowanie aplikacji .NET API...${NC}"

# Przejdź do katalogu API
cd src/RAG.Orchestrator.Api

# Restore packages
echo -e "${YELLOW}Przywracanie pakietów NuGet...${NC}"
dotnet restore

# Zbuduj aplikację w trybie Release
echo -e "${YELLOW}Budowanie aplikacji .NET...${NC}"
dotnet publish -c Release -o ../../build/api

# Sprawdź czy build się powiódł
if [ -f "../../build/api/RAG.Orchestrator.Api.dll" ]; then
    echo -e "${GREEN}✓ Aplikacja .NET zbudowana pomyślnie${NC}"
else
    echo -e "${RED}✗ Błąd budowania aplikacji .NET${NC}"
    exit 1
fi

echo -e "${BLUE}[5/8] Budowanie aplikacji React (Web.UI)...${NC}"

# Przejdź do katalogu React
cd ../RAG.Web.UI

# Sprawdź czy Node.js jest dostępny
NODE_AVAILABLE=false
if command -v node &> /dev/null && command -v npm &> /dev/null; then
    NODE_AVAILABLE=true
    echo -e "${BLUE}Używam Node.js $(node --version) i npm $(npm --version)${NC}"
else
    echo -e "${YELLOW}⚠ Node.js lub npm nie jest dostępny - pomijam budowanie Web UI${NC}"
    echo -e "${YELLOW}Web UI nie będzie dostępne. API będzie działać normalnie.${NC}"
fi

if [ "$NODE_AVAILABLE" = true ]; then
    # Sprawdź czy node_modules istnieje
    if [ ! -d "node_modules" ]; then
        echo -e "${YELLOW}Instalowanie zależności npm...${NC}"
        if ! npm install; then
            echo -e "${YELLOW}⚠ Błąd instalacji npm - pomijam budowanie Web UI${NC}"
            NODE_AVAILABLE=false
        fi
    else
        echo -e "${YELLOW}Aktualizowanie zależności npm...${NC}"
        if ! npm install; then
            echo -e "${YELLOW}⚠ Błąd aktualizacji npm - próbuję budować z istniejącymi zależnościami${NC}"
        fi
    fi

    if [ "$NODE_AVAILABLE" = true ]; then
        # Build aplikacji React
        echo -e "${YELLOW}Budowanie aplikacji React...${NC}"
        if npm run build && [ -d "dist" ]; then
            # Kopiowanie build do głównego katalogu build
            cp -r dist/* ../../build/web/
            echo -e "${GREEN}✓ React app zbudowana pomyślnie${NC}"
        else
            echo -e "${YELLOW}⚠ Błąd budowania React app - pomijam Web UI${NC}"
        fi
    fi
fi

# Przejdź z powrotem do głównego katalogu
cd ../..

echo -e "${BLUE}[6/8] Konfiguracja produkcyjna...${NC}"

# Sprawdź czy istnieje plik konfiguracyjny
if [ ! -f "build/api/appsettings.Production.json" ]; then
    echo -e "${YELLOW}Tworzenie domyślnego pliku konfiguracyjnego...${NC}"
    cat > build/api/appsettings.Production.json << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "ElasticSearch": "http://localhost:9200"
  },
  "LLM": {
    "ApiUrl": "http://localhost:11434",
    "Model": "llama3.1",
    "Temperature": 0.7,
    "MaxTokens": 2000
  },
  "RagSettings": {
    "ChunkSize": 1000,
    "ChunkOverlap": 200,
    "MaxResults": 10
  },
  "Security": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://asystent.ad.citronex.pl",
      "https://asystent.ad.citronex.pl"
    ]
  }
}
EOF
    echo -e "${GREEN}✓ Utworzono domyślny plik konfiguracyjny${NC}"
else
    echo -e "${GREEN}✓ Plik konfiguracyjny już istnieje${NC}"
fi

echo -e "${BLUE}[7/8] Ustawianie uprawnień...${NC}"

# Ustaw właściciela na www-data
chown -R $APP_USER:$APP_USER build/

# Ustaw uprawnienia dla plików API
find build/api -type f -exec chmod 644 {} \;
find build/api -type d -exec chmod 755 {} \;

# Ustaw uprawnienia wykonania dla głównego pliku API
chmod +x build/api/RAG.Orchestrator.Api.dll

# Ustaw uprawnienia dla plików Web
find build/web -type f -exec chmod 644 {} \;
find build/web -type d -exec chmod 755 {} \;

echo -e "${GREEN}✓ Uprawnienia ustawione dla $APP_USER${NC}"

echo -e "${BLUE}[8/8] Uruchamianie serwisów...${NC}"

# Uruchom serwis RAG API
systemctl daemon-reload
systemctl enable rag-api
systemctl start rag-api

# Sprawdź czy serwis startuje poprawnie
sleep 3
if systemctl is-active --quiet rag-api; then
    echo -e "${GREEN}✓ RAG API uruchomiony pomyślnie${NC}"
else
    echo -e "${RED}✗ Błąd uruchamiania RAG API${NC}"
    echo -e "${YELLOW}Sprawdź logi: sudo journalctl -u rag-api -f${NC}"
    exit 1
fi

# Sprawdź czy nginx działa
if systemctl is-active --quiet nginx; then
    echo -e "${GREEN}✓ Nginx działa${NC}"
    systemctl reload nginx
else
    echo -e "${YELLOW}Uruchamianie Nginx...${NC}"
    systemctl start nginx
    if systemctl is-active --quiet nginx; then
        echo -e "${GREEN}✓ Nginx uruchomiony${NC}"
    else
        echo -e "${RED}✗ Błąd uruchamiania Nginx${NC}"
        exit 1
    fi
fi

echo -e "${BLUE}[TEST] Sprawdzanie połączenia...${NC}"

# Test API
echo -n -e "${YELLOW}Test API (http://localhost:5000/health)... ${NC}"
if curl -f -s http://localhost:5000/health > /dev/null; then
    echo -e "${GREEN}✓ OK${NC}"
else
    echo -e "${RED}✗ FAIL${NC}"
    echo -e "${YELLOW}⚠ API może potrzebować więcej czasu na uruchomienie${NC}"
fi

# Test proxy Nginx do API
echo -n -e "${YELLOW}Test proxy Nginx (http://localhost/health)... ${NC}"
if curl -f -s -I http://localhost/health > /dev/null; then
    echo -e "${GREEN}✓ OK${NC}"
else
    echo -e "${YELLOW}⚠ Problem z proxy Nginx do API${NC}"
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}    Deployment zakończony pomyślnie!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}Status serwisów:${NC}"
echo "- RAG API: $(systemctl is-active rag-api)"
echo "- Nginx: $(systemctl is-active nginx)"
echo ""
echo -e "${YELLOW}Aplikacja dostępna pod adresem:${NC}"
DOMAIN_IP=$(hostname -I | awk '{print $1}')
echo "- Frontend: http://asystent.ad.citronex.pl (lub http://$DOMAIN_IP)"
echo "- API Health: http://asystent.ad.citronex.pl/health"
echo "- System Health: http://asystent.ad.citronex.pl/healthz/system"
echo ""
echo -e "${YELLOW}Monitoring i diagnostyka:${NC}"
echo "- Logi API: sudo journalctl -fu rag-api"
echo "- Logi Nginx: sudo tail -f /var/log/nginx/rag-suite.error.log"
echo "- Status serwisów: sudo systemctl status rag-api nginx"
echo "- Test konfiguracji: sudo nginx -t"
echo "- Nginx status: curl http://localhost/nginx-status (tylko lokalnie)"
echo ""
echo -e "${YELLOW}Zarządzanie:${NC}"
echo "- Restart API: sudo systemctl restart rag-api"
echo "- Reload Nginx: sudo systemctl reload nginx"
echo "- Ponowny deployment: sudo ./deploy.sh"
echo ""

# Pokaż informacje o buildzie
echo -e "${YELLOW}Informacje o buildzie:${NC}"
echo "- Branch: $(git branch --show-current)"
echo "- Commit: $(git rev-parse --short HEAD)"
echo "- Data: $(date)"
echo "- API Size: $(du -sh build/api | cut -f1)"
echo "- Web Size: $(du -sh build/web | cut -f1)"
echo
