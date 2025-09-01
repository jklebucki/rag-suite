#!/bin/bash

# RAG Suite - Fix Nginx Timeouts for Chat Operations
# Updates existing nginx configuration to support 15-minute chat operations

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

APP_NAME="rag-suite"
NGINX_CONFIG="/etc/nginx/sites-available/$APP_NAME"

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}  RAG Suite - Fix Nginx Timeouts${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}Ten skrypt musi być uruchomiony jako root (użyj sudo)${NC}"
   exit 1
fi

# Check if nginx config exists
if [ ! -f "$NGINX_CONFIG" ]; then
    echo -e "${RED}❌ Konfiguracja nginx nie znaleziona: $NGINX_CONFIG${NC}"
    echo -e "${YELLOW}Uruchom najpierw: sudo ./nginx-setup.sh${NC}"
    exit 1
fi

echo -e "${BLUE}[1/3] Backup aktualnej konfiguracji...${NC}"
cp "$NGINX_CONFIG" "$NGINX_CONFIG.backup.$(date +%Y%m%d_%H%M%S)"
echo -e "${GREEN}✓ Backup utworzony${NC}"

echo -e "${BLUE}[2/3] Aktualizacja timeoutów nginx...${NC}"

# Fix timeouts in nginx config
sed -i 's/proxy_send_timeout 30s;/proxy_send_timeout 900s;/g' "$NGINX_CONFIG"
sed -i 's/proxy_read_timeout 30s;/proxy_read_timeout 900s;/g' "$NGINX_CONFIG"

echo -e "${GREEN}✓ Timeouty zaktualizowane:${NC}"
echo -e "${GREEN}  - proxy_send_timeout: 30s → 900s (15 min)${NC}"
echo -e "${GREEN}  - proxy_read_timeout: 30s → 900s (15 min)${NC}"

echo -e "${BLUE}[3/3] Testowanie i przeładowanie nginx...${NC}"

# Test nginx configuration
if nginx -t; then
    echo -e "${GREEN}✓ Konfiguracja nginx poprawna${NC}"
    
    # Reload nginx
    systemctl reload nginx
    echo -e "${GREEN}✓ Nginx przeładowany${NC}"
else
    echo -e "${RED}❌ Błąd w konfiguracji nginx${NC}"
    echo -e "${YELLOW}Przywracanie backup...${NC}"
    cp "$NGINX_CONFIG.backup.$(date +%Y%m%d_%H%M%S)" "$NGINX_CONFIG"
    exit 1
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}        ✅ GOTOWE!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${GREEN}Nginx skonfigurowany z 15-minutowymi timeoutami dla chat operacji${NC}"
echo -e "${YELLOW}Możesz teraz przetestować długie operacje chat${NC}"
echo ""
