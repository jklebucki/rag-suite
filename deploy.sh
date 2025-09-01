#!/bin/bash

# RAG Suite Deployment Script
# Aktualizuje kod z git, buduje aplikację .NET i React, wdraża na produkcję

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
   echo -e "${RED}Katalog aplikacji $APP_DIR nie istnieje!${NC}"
   echo -e "${RED}Najpierw uruchom production-setup.sh${NC}"
   exit 1
fi

cd $APP_DIR

echo -e "${BLUE}[1/8] Zatrzymywanie serwisów...${NC}"

# Zatrzymaj API jeśli działa
if systemctl is-active --quiet rag-api; then
    echo -e "${YELLOW}Zatrzymywanie RAG API...${NC}"
    systemctl stop rag-api
    sleep 2
fi

echo -e "${BLUE}[2/8] Aktualizacja kodu z Git...${NC}"

# Sprawdź status git
echo -e "${YELLOW}Status git przed aktualizacją:${NC}"
git status --porcelain

# Zapisz lokalne zmiany jeśli istnieją
if [ -n "$(git status --porcelain)" ]; then
    echo -e "${YELLOW}Zapisywanie lokalnych zmian...${NC}"
    git stash push -m "Auto-stash before deployment $(date)"
fi

# Pobierz najnowsze zmiany
echo -e "${YELLOW}Pobieranie zmian z repozytorium...${NC}"
git fetch origin
git checkout $GIT_BRANCH
git pull origin $GIT_BRANCH

echo -e "${GREEN}✓ Kod zaktualizowany pomyślnie${NC}"

echo -e "${BLUE}[3/8] Przygotowanie katalogów build...${NC}"

# Usuń stary build
rm -rf build
mkdir -p build/api
mkdir -p build/web

echo -e "${BLUE}[4/8] Budowanie aplikacji .NET API...${NC}"

cd src/RAG.Orchestrator.Api

# Restore packages
echo -e "${YELLOW}Przywracanie pakietów NuGet...${NC}"
dotnet restore

# Build w trybie Release
echo -e "${YELLOW}Budowanie API w trybie Release...${NC}"
dotnet build --configuration Release --no-restore

# Publish do katalogu build
echo -e "${YELLOW}Publikowanie API...${NC}"
dotnet publish --configuration Release --no-build --output "../../build/api"

cd ../../

if [ -f "build/api/RAG.Orchestrator.Api.dll" ]; then
    echo -e "${GREEN}✓ API zbudowane pomyślnie${NC}"
else
    echo -e "${RED}✗ Błąd podczas budowania API${NC}"
    exit 1
fi

echo -e "${BLUE}[5/8] Budowanie aplikacji React (Web.UI)...${NC}"

cd src/RAG.Web.UI

# Sprawdź czy node_modules istnieje
if [ ! -d "node_modules" ]; then
    echo -e "${YELLOW}Instalowanie zależności npm...${NC}"
    npm install
else
    echo -e "${YELLOW}Aktualizowanie zależności npm...${NC}"
    npm install
fi

# Build aplikacji React
echo -e "${YELLOW}Budowanie aplikacji React...${NC}"
npm run build

# Kopiowanie build do głównego katalogu build
if [ -d "dist" ]; then
    cp -r dist/* ../../build/web/
    echo -e "${GREEN}✓ React app zbudowana pomyślnie${NC}"
else
    echo -e "${RED}✗ Błąd podczas budowania React app${NC}"
    exit 1
fi

cd ../../

echo -e "${BLUE}[6/8] Konfiguracja produkcyjna...${NC}"

# Tworzenie appsettings.Production.json jeśli nie istnieje
if [ ! -f "build/api/appsettings.Production.json" ]; then
    echo -e "${YELLOW}Tworzenie appsettings.Production.json...${NC}"
    cat > build/api/appsettings.Production.json << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    },
    "Console": {
      "FormatterName": "json"
    },
    "File": {
      "Path": "/var/log/rag-suite/app.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 30
    }
  },
  "AllowedHosts": "asystent.ad.citronex.pl,localhost",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    },
    "Limits": {
      "MaxConcurrentConnections": 100,
      "MaxConcurrentUpgradedConnections": 100,
      "MaxRequestBodySize": 30000000
    }
  },
  "ConnectionStrings": {
    "ElasticSearch": "http://elasticsearch.ad.citronex.pl:9200"
  },
  "LLM": {
    "Provider": "Ollama",
    "BaseUrl": "http://llm.ad.citronex.pl:11434",
    "Model": "llama3.1:8b",
    "Timeout": "00:05:00"
  },
  "CORS": {
    "AllowedOrigins": [
      "http://asystent.ad.citronex.pl",
      "https://asystent.ad.citronex.pl"
    ]
  },
  "Security": {
    "RequireHttps": false,
    "EnableRateLimiting": true,
    "MaxRequestsPerMinute": 60
  }
}
EOF
else
    echo -e "${GREEN}✓ appsettings.Production.json już istnieje${NC}"
fi

# Tworzenie web.config dla IIS (jeśli potrzebne w przyszłości)
cat > build/web/web.config << EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="React Routes" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
            <add input="{REQUEST_URI}" pattern="^/(api)" negate="true" />
          </conditions>
          <action type="Rewrite" url="/" />
        </rule>
      </rules>
    </rewrite>
    <staticContent>
      <mimeMap fileExtension=".json" mimeType="application/json" />
    </staticContent>
  </system.webServer>
</configuration>
EOF

echo -e "${BLUE}[7/8] Ustawianie uprawnień...${NC}"

# Ustaw właściciela wszystkich plików
chown -R $APP_USER:$APP_USER $APP_DIR

# Ustaw odpowiednie uprawnienia
chmod -R 755 $APP_DIR
chmod +x build/api/RAG.Orchestrator.Api

# Specjalne uprawnienia dla plików web
find build/web -type f -exec chmod 644 {} \;
find build/web -type d -exec chmod 755 {} \;

# Uprawnienia dla logów
mkdir -p /var/log/rag-suite
chown -R $APP_USER:$APP_USER /var/log/rag-suite
chmod -R 755 /var/log/rag-suite

echo -e "${BLUE}[8/8] Uruchamianie serwisów...${NC}"

# Przeładuj konfigurację systemd
systemctl daemon-reload

# Uruchom API
echo -e "${YELLOW}Uruchamianie RAG API...${NC}"
systemctl start rag-api

# Sprawdź czy API się uruchomił
sleep 3
if systemctl is-active --quiet rag-api; then
    echo -e "${GREEN}✓ RAG API uruchomiony pomyślnie${NC}"
else
    echo -e "${RED}✗ Problem z uruchomieniem RAG API${NC}"
    echo -e "${YELLOW}Logi serwisu:${NC}"
    journalctl -u rag-api --no-pager -n 20
    exit 1
fi

# Przeładuj nginx (dla nowych plików statycznych)
echo -e "${YELLOW}Przeładowywanie Nginx...${NC}"
systemctl reload nginx

if systemctl is-active --quiet nginx; then
    echo -e "${GREEN}✓ Nginx przeładowany pomyślnie${NC}"
else
    echo -e "${RED}✗ Problem z Nginx${NC}"
    systemctl status nginx --no-pager
    exit 1
fi

# Test połączenia z API
echo -e "${YELLOW}Testowanie API...${NC}"
sleep 3

# Sprawdź podstawowy health check
if curl -f -s http://localhost:5000/health > /dev/null; then
    echo -e "${GREEN}✓ API Health Check - OK${NC}"
    
    # Sprawdź system health jeśli dostępny
    if curl -f -s http://localhost:5000/healthz/system > /dev/null; then
        echo -e "${GREEN}✓ System Health Check - OK${NC}"
    fi
else
    echo -e "${YELLOW}⚠ API może jeszcze się uruchamiać lub wystąpił problem${NC}"
    echo -e "${YELLOW}Sprawdzanie logów...${NC}"
    journalctl -u rag-api --no-pager -n 10
fi

# Test czy strona główna odpowiada
if curl -f -s -I http://localhost/health > /dev/null; then
    echo -e "${GREEN}✓ Nginx proxy do API - OK${NC}"
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
