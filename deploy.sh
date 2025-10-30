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

# Utwórz podstawowy index.html jeśli Web UI nie będzie zbudowane
cat > build/web/index.html << 'EOF'
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>RAG Suite</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; text-align: center; }
        .container { max-width: 600px; margin: 0 auto; }
        .status { padding: 20px; background: #f0f0f0; border-radius: 8px; margin: 20px 0; }
        .api-link { color: #007acc; text-decoration: none; }
        .api-link:hover { text-decoration: underline; }
    </style>
</head>
<body>
    <div class="container">
        <h1>RAG Suite</h1>
        <div class="status">
            <h2>API Status</h2>
            <p>Web UI is not available (Node.js required for building)</p>
            <p>API is running at: <a href="/api" class="api-link">API Endpoint</a></p>
            <p>Health check: <a href="/health" class="api-link">Health Check</a></p>
        </div>
        <p>To build the full Web UI, install Node.js and run the deployment script again.</p>
    </div>
</body>
</html>
EOF

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
        # Sprawdź czy Node.js jest z snap (ma ograniczenia katalogów)
        if command -v snap >/dev/null 2>&1 && snap list node >/dev/null 2>&1; then
            echo -e "${YELLOW}⚠ Wykryto Node.js z snap - budowanie w /tmp${NC}"
            TEMP_BUILD_DIR="/tmp/rag-ui-build-$$"
            mkdir -p "$TEMP_BUILD_DIR"
            
            # Kopiuj źródła do temp
            cp -r . "$TEMP_BUILD_DIR/"
            cd "$TEMP_BUILD_DIR"
            
            # Build aplikacji React w temp
            echo -e "${YELLOW}Budowanie aplikacji React w /tmp...${NC}"
            if npm install && npm run build; then
                if [ -d "dist" ] && [ "$(ls -A dist 2>/dev/null)" ]; then
                    # Wyczyść stary build
                    rm -rf /var/www/rag-suite/build/web/*
                    # Kopiowanie build do głównego katalogu build
                    cp -r dist/* /var/www/rag-suite/build/web/
                    echo -e "${GREEN}✓ React app zbudowana pomyślnie (przez /tmp)${NC}"
                    echo -e "${GREEN}✓ Pliki skopiowane do build/web${NC}"
                else
                    echo -e "${YELLOW}⚠ Katalog dist jest pusty lub nie istnieje${NC}"
                    echo -e "${YELLOW}Zachowuję podstawowy index.html${NC}"
                fi
            else
                echo -e "${YELLOW}⚠ Błąd budowania React app w /tmp${NC}"
                echo -e "${YELLOW}Zachowuję podstawowy index.html${NC}"
            fi
            
            # Wyczyść temp
            cd /var/www/rag-suite/src/RAG.Web.UI
            rm -rf "$TEMP_BUILD_DIR"
        else
            # Build aplikacji React normalnie
            echo -e "${YELLOW}Budowanie aplikacji React...${NC}"
            if npm run build; then
                if [ -d "dist" ] && [ "$(ls -A dist 2>/dev/null)" ]; then
                    # Wyczyść stary build
                    rm -rf ../../build/web/*
                    # Kopiowanie build do głównego katalogu build
                    cp -r dist/* ../../build/web/
                    echo -e "${GREEN}✓ React app zbudowana pomyślnie${NC}"
                    echo -e "${GREEN}✓ Pliki skopiowane do build/web${NC}"
                else
                    echo -e "${YELLOW}⚠ Katalog dist jest pusty lub nie istnieje${NC}"
                    echo -e "${YELLOW}Zachowuję podstawowy index.html${NC}"
                fi
            else
                echo -e "${YELLOW}⚠ Błąd budowania React app${NC}"
                echo -e "${YELLOW}Zachowuję podstawowy index.html${NC}"
            fi
        fi
    else
        echo -e "${YELLOW}⚠ Node.js niedostępny - używam podstawowego index.html${NC}"
    fi
fi

# Przejdź z powrotem do głównego katalogu
cd ../..

# Sprawdź rezultat budowania Web UI
echo -e "${BLUE}[6/8] Sprawdzanie Web UI...${NC}"
if [ -f "build/web/index.html" ]; then
    WEB_SIZE=$(du -sh build/web 2>/dev/null | cut -f1)
    FILE_COUNT=$(find build/web -type f | wc -l | tr -d ' ')
    echo -e "${GREEN}✓ Web UI dostępne (${FILE_COUNT} plików, ${WEB_SIZE})${NC}"
    
    # Sprawdź czy to pełny build czy podstawowy
    if grep -q "Web UI is not available" build/web/index.html; then
        echo -e "${YELLOW}⚠ Używa podstawowego index.html (Node.js wymagany dla pełnego UI)${NC}"
    else
        echo -e "${GREEN}✓ Pełny build React UI${NC}"
    fi
else
    echo -e "${RED}✗ Brak pliku index.html w build/web${NC}"
fi

echo -e "${BLUE}[7/8] Konfiguracja produkcyjna...${NC}"

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
  "Services": {
    "Elasticsearch": {
      "Url": "http://localhost:9200",
      "Username": "elastic",
      "Password": "elastic",
      "TimeoutMinutes": 10
    },
    "EmbeddingService": {
      "Url": "http://192.168.21.14:8580"
    },
    "LlmService": {
      "Url": "http://192.168.21.14:11434",
      "MaxTokens": 300,
      "Temperature": 0.2,
      "Model": "gpt-oss:20b",
      "IsOllama": true,
      "TimeoutMinutes": 10
    }
  },
  "Chat": {
    "MaxMessageLength": 2000,
    "MaxMessagesPerSession": 100,
    "SessionTimeoutMinutes": 60
  },
  "DefaultAdmin": {
    "Email": "admin@citronex.pl",
    "Password": "Citro@123"
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

echo -e "${BLUE}[8/9] Ustawianie uprawnień...${NC}"

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

echo -e "${BLUE}[9/9] Uruchamianie serwisów...${NC}"

# Przeładuj konfigurację systemd
systemctl daemon-reload

# Sprawdź czy serwis był już uruchomiony
WAS_RUNNING=false
if systemctl is-active --quiet rag-api; then
    WAS_RUNNING=true
fi

# Uruchom serwis RAG API
systemctl enable rag-api
echo -e "${YELLOW}Uruchamianie RAG API (timeout 60s)...${NC}"
systemctl start rag-api

# Sprawdź czy serwis startuje poprawnie (z większym timeout)
echo -e "${YELLOW}Oczekiwanie na uruchomienie serwisu...${NC}"
for i in {1..12}; do
    sleep 5
    if systemctl is-active --quiet rag-api; then
        echo -e "${GREEN}✓ RAG API uruchomiony pomyślnie (${i}x5s)${NC}"
        break
    else
        echo -n "."
        if [ $i -eq 12 ]; then
            echo ""
            echo -e "${RED}✗ Timeout uruchamiania RAG API${NC}"
            echo -e "${YELLOW}Status serwisu:${NC}"
            systemctl status rag-api --no-pager -l
            echo -e "${YELLOW}Sprawdź logi: sudo journalctl -u rag-api -f${NC}"
            exit 1
        fi
    fi
done

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
echo -n -e "${YELLOW}Test API (http://localhost:5000/healthz/system)... ${NC}"
if curl -f -s http://localhost:5000/healthz/system > /dev/null; then
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
