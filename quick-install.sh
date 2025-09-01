#!/bin/bash

# RAG Suite Quick Install Script
# Jednokomendowa instalacja całego systemu RAG Suite

set -e

# Kolory dla outputu
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Zmienne konfiguracyjne
DOMAIN_NAME="${1:-asystent.ad.citronex.pl}"
EMAIL="${2:-admin@citronex.pl}"
GIT_REPO="https://github.com/jklebucki/rag-suite.git"
INSTALL_SSL="${3:-n}"

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}    RAG Suite Quick Install${NC}"
echo -e "${BLUE}========================================${NC}"
echo -e "${YELLOW}Domena: ${DOMAIN_NAME}${NC}"
echo -e "${YELLOW}Email: ${EMAIL}${NC}"
echo -e "${YELLOW}SSL: ${INSTALL_SSL}${NC}"
echo ""

# Sprawdź czy skrypt jest uruchamiany jako root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}Ten skrypt musi być uruchomiony jako root (użyj sudo)${NC}"
   echo -e "${YELLOW}Użycie: sudo bash quick-install.sh [DOMAIN] [EMAIL] [SSL:y/n]${NC}"
   exit 1
fi

echo -e "${BLUE}[1/4] Pobieranie i uruchamianie skryptu setup...${NC}"

# Przejdź do katalogu tmp
cd /tmp

# Pobierz skrypt setup
curl -sSL https://raw.githubusercontent.com/jklebucki/rag-suite/main/production-setup.sh -o production-setup.sh
chmod +x production-setup.sh

# Uruchom skrypt setup
./production-setup.sh $DOMAIN_NAME $GIT_REPO

echo -e "${BLUE}[2/4] Deployment aplikacji...${NC}"

# Przejdź do katalogu aplikacji
cd /var/www/rag-suite

# Uruchom deployment
./deploy.sh

echo -e "${BLUE}[3/4] Konfiguracja SSL...${NC}"

if [[ $INSTALL_SSL =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Instalacja SSL dla domeny $DOMAIN_NAME...${NC}"
    ./ssl-setup.sh
else
    echo -e "${YELLOW}Pomijanie konfiguracji SSL (można uruchomić później)${NC}"
    echo -e "${YELLOW}Aby skonfigurować SSL uruchom: cd /var/www/rag-suite && sudo ./ssl-setup.sh${NC}"
fi

echo -e "${BLUE}[4/4] Finalizacja...${NC}"

# Sprawdź status serwisów
echo -e "${YELLOW}Status serwisów:${NC}"
systemctl is-active rag-api && echo -e "${GREEN}✓ RAG API: $(systemctl is-active rag-api)${NC}" || echo -e "${RED}✗ RAG API: $(systemctl is-active rag-api)${NC}"
systemctl is-active nginx && echo -e "${GREEN}✓ Nginx: $(systemctl is-active nginx)${NC}" || echo -e "${RED}✗ Nginx: $(systemctl is-active nginx)${NC}"

# Test połączenia
echo -e "${YELLOW}Test połączenia...${NC}"
sleep 2

PROTOCOL="http"
if [[ $INSTALL_SSL =~ ^[Yy]$ ]]; then
    PROTOCOL="https"
fi

if curl -f -s -I $PROTOCOL://$DOMAIN_NAME/health > /dev/null; then
    echo -e "${GREEN}✓ Aplikacja odpowiada na żądania${NC}"
else
    echo -e "${YELLOW}⚠ Sprawdź konfigurację DNS lub poczekaj chwilę${NC}"
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}    Instalacja zakończona!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}Aplikacja RAG Suite jest dostępna pod adresem:${NC}"
echo -e "${GREEN}🌐 $PROTOCOL://$DOMAIN_NAME${NC}"
echo ""
echo -e "${YELLOW}Health Check:${NC}"
echo "- API: $PROTOCOL://$DOMAIN_NAME/health"
echo "- System: $PROTOCOL://$DOMAIN_NAME/healthz/system"
echo ""
echo -e "${YELLOW}Zarządzanie:${NC}"
echo "- Aktualizacja: cd /var/www/rag-suite && sudo ./deploy.sh"
echo "- Logi API: sudo journalctl -fu rag-api"
echo "- Logi Nginx: sudo tail -f /var/log/nginx/rag-suite.error.log"
echo "- Status: sudo systemctl status rag-api nginx"
echo ""
echo -e "${YELLOW}Pliki konfiguracyjne:${NC}"
echo "- API: /var/www/rag-suite/build/api/appsettings.Production.json"
echo "- Nginx: /etc/nginx/sites-available/rag-suite"
echo "- Serwis: /etc/systemd/system/rag-api.service"
echo ""

if [[ ! $INSTALL_SSL =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}💡 Aby skonfigurować SSL (HTTPS):${NC}"
    echo "cd /var/www/rag-suite && sudo ./ssl-setup.sh"
    echo ""
fi

echo -e "${BLUE}Dziękujemy za wybór RAG Suite! 🚀${NC}"
