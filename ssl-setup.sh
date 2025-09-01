#!/bin/bash

# RAG Suite SSL Setup Script
# Configures SSL/HTTPS for domain using existing certificates

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
DOMAIN_NAME="asystent.ad.citronex.pl"
SSL_CERT_PATH="/home/selfsigned_ad/ad.citronex.pl.pem"
SSL_KEY_PATH="/home/selfsigned_ad/ad.citronex.pl.key"
APP_NAME="rag-suite"

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}    RAG Suite SSL Configuration${NC}"
echo -e "${BLUE}========================================${NC}"
echo -e "${YELLOW}Domain: ${DOMAIN_NAME}${NC}"
echo -e "${YELLOW}Certificate: ${SSL_CERT_PATH}${NC}"
echo -e "${YELLOW}Private Key: ${SSL_KEY_PATH}${NC}"
echo ""

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}Ten skrypt musi być uruchomiony jako root (użyj sudo)${NC}"
   exit 1
fi

# Check if nginx is running
if ! systemctl is-active --quiet nginx; then
    echo -e "${RED}Nginx nie działa. Uruchom najpierw production-setup.sh${NC}"
    exit 1
fi

echo -e "${BLUE}[1/3] Sprawdzanie certyfikatów...${NC}"

# Check if certificates exist
if [ ! -f "$SSL_CERT_PATH" ]; then
    echo -e "${RED}Plik certyfikatu nie istnieje: $SSL_CERT_PATH${NC}"
    echo -e "${YELLOW}Upewnij się, że certyfikaty wildcard *.ad.citronex.pl są dostępne${NC}"
    exit 1
fi

if [ ! -f "$SSL_KEY_PATH" ]; then
    echo -e "${RED}Plik klucza nie istnieje: $SSL_KEY_PATH${NC}"
    echo -e "${YELLOW}Upewnij się, że certyfikaty wildcard *.ad.citronex.pl są dostępne${NC}"
    exit 1
fi

# Test certificate
if openssl x509 -in "$SSL_CERT_PATH" -text -noout | grep -q "CN=\*.ad.citronex.pl"; then
    echo -e "${GREEN}✓ Certyfikat wildcard *.ad.citronex.pl znaleziony${NC}"
else
    echo -e "${YELLOW}⚠ Ostrzeżenie: Certyfikat może nie pasować do domeny${NC}"
fi

echo -e "${BLUE}[2/3] Aktualizacja konfiguracji nginx z SSL...${NC}"

# Use nginx-setup.sh with SSL enabled
export DOMAIN_NAME
export SSL_CERT_PATH
export SSL_KEY_PATH

# Run nginx setup (it will detect SSL certificates and configure HTTPS)
./nginx-setup.sh

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Konfiguracja nginx z SSL zaktualizowana${NC}"
else
    echo -e "${RED}✗ Błąd aktualizacji konfiguracji nginx${NC}"
    exit 1
fi

echo -e "${BLUE}[3/3] Test konfiguracji SSL...${NC}"

# Test SSL configuration
if nginx -t; then
    echo -e "${GREEN}✓ Konfiguracja nginx jest poprawna${NC}"
    
    # Reload nginx
    systemctl reload nginx
    echo -e "${GREEN}✓ Nginx przeładowany z SSL${NC}"
else
    echo -e "${RED}✗ Błąd w konfiguracji nginx${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}    SSL skonfigurowany pomyślnie!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}Informacje o SSL:${NC}"
echo "- Certyfikat: $SSL_CERT_PATH"
echo "- Klucz prywatny: $SSL_KEY_PATH"
echo "- Domena: $DOMAIN_NAME"
echo ""
echo -e "${YELLOW}URL-e:${NC}"
echo "- Frontend: https://$DOMAIN_NAME"
echo "- API Health: https://$DOMAIN_NAME/health"
echo "- System Health: https://$DOMAIN_NAME/healthz/system"
echo ""
echo -e "${YELLOW}Testy SSL:${NC}"
echo "curl -I https://$DOMAIN_NAME"
echo "openssl s_client -connect $DOMAIN_NAME:443 -servername $DOMAIN_NAME"
echo ""
echo -e "${YELLOW}Monitoring:${NC}"
echo "- Test nginx: sudo nginx -t"
echo "- Status nginx: sudo systemctl status nginx"
echo "- Logi nginx: sudo tail -f /var/log/nginx/error.log"
echo "- SSL logs: sudo tail -f /var/log/nginx/ssl.log"
echo ""
