#!/bin/bash

# RAG Suite Health Check Script
# Sprawdza czy wszystkie komponenty aplikacji działają poprawnie

set -e

# Kolory dla outputu
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

DOMAIN_NAME="asystent.ad.citronex.pl"
API_PORT="5000"
FAILED_CHECKS=0

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}    RAG Suite Health Check${NC}"
echo -e "${BLUE}========================================${NC}"
echo

# Funkcja sprawdzania
check_service() {
    local service_name=$1
    local test_command=$2
    local expected_result=$3
    
    echo -n -e "${YELLOW}[TEST] $service_name... ${NC}"
    
    if eval "$test_command" &>/dev/null; then
        echo -e "${GREEN}✓ OK${NC}"
    else
        echo -e "${RED}✗ FAIL${NC}"
        ((FAILED_CHECKS++))
        if [ -n "$expected_result" ]; then
            echo -e "  ${RED}Expected: $expected_result${NC}"
        fi
    fi
}

echo -e "${BLUE}=== SYSTEM CHECKS ===${NC}"

# Sprawdź podstawowe narzędzia
check_service "dotnet CLI" "command -v dotnet"
check_service "Node.js" "command -v node"
check_service "npm" "command -v npm"
check_service "nginx" "command -v nginx"

echo
echo -e "${BLUE}=== SERVICE CHECKS ===${NC}"

# Sprawdź serwisy systemd
check_service "nginx service" "systemctl is-active --quiet nginx"
check_service "rag-api service" "systemctl is-active --quiet rag-api"

echo
echo -e "${BLUE}=== NETWORK CHECKS ===${NC}"

# Sprawdź porty
check_service "Port 80 (HTTP)" "netstat -tulpn | grep -q ':80 '"
check_service "Port 443 (HTTPS)" "netstat -tulpn | grep -q ':443 '"
check_service "Port 5000 (API)" "netstat -tulpn | grep -q ':5000 '"

echo
echo -e "${BLUE}=== APPLICATION CHECKS ===${NC}"

# Sprawdź API
check_service "API Health Check" "curl -s http://localhost:5000/health | grep -q 'Healthy\\|OK\\|Up'"

# Sprawdź czy frontend jest dostępny
check_service "Frontend files" "test -f /var/www/rag-suite/build/web/index.html"

# Sprawdź konfigurację nginx
check_service "Nginx config test" "nginx -t"

echo
echo -e "${BLUE}=== HTTP TESTS ===${NC}"

# Test HTTP
echo -n -e "${YELLOW}[TEST] HTTP Response (80)... ${NC}"
HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost/ 2>/dev/null || echo "000")
if [[ "$HTTP_STATUS" =~ ^(200|301|302)$ ]]; then
    echo -e "${GREEN}✓ OK ($HTTP_STATUS)${NC}"
else
    echo -e "${RED}✗ FAIL ($HTTP_STATUS)${NC}"
    ((FAILED_CHECKS++))
fi

# Test HTTPS (jeśli skonfigurowane)
if netstat -tulpn | grep -q ':443 '; then
    echo -n -e "${YELLOW}[TEST] HTTPS Response (443)... ${NC}"
    HTTPS_STATUS=$(curl -s -k -o /dev/null -w "%{http_code}" https://localhost/ 2>/dev/null || echo "000")
    if [[ "$HTTPS_STATUS" =~ ^(200|301|302)$ ]]; then
        echo -e "${GREEN}✓ OK ($HTTPS_STATUS)${NC}"
    else
        echo -e "${RED}✗ FAIL ($HTTPS_STATUS)${NC}"
        ((FAILED_CHECKS++))
    fi
fi

echo
echo -e "${BLUE}=== DISK & PERMISSIONS ===${NC}"

# Sprawdź miejsce na dysku
DISK_USAGE=$(df /var/www | awk 'NR==2 {print $5}' | sed 's/%//')
echo -n -e "${YELLOW}[TEST] Disk space... ${NC}"
if [ "$DISK_USAGE" -lt 90 ]; then
    echo -e "${GREEN}✓ OK (${DISK_USAGE}% used)${NC}"
else
    echo -e "${RED}✗ FAIL (${DISK_USAGE}% used - low space!)${NC}"
    ((FAILED_CHECKS++))
fi

# Sprawdź uprawnienia
check_service "App directory permissions" "test -d /var/www/rag-suite && test -r /var/www/rag-suite"
check_service "Build directory permissions" "test -d /var/www/rag-suite/build && test -r /var/www/rag-suite/build"

echo
echo -e "${BLUE}=== LOG FILES ===${NC}"

# Sprawdź logi
echo -n -e "${YELLOW}[INFO] Recent API errors: ${NC}"
API_ERRORS=$(sudo journalctl -u rag-api --since "1 hour ago" | grep -i error | wc -l)
if [ "$API_ERRORS" -eq 0 ]; then
    echo -e "${GREEN}0${NC}"
else
    echo -e "${YELLOW}$API_ERRORS${NC}"
fi

echo -n -e "${YELLOW}[INFO] Recent Nginx errors: ${NC}"
if [ -f /var/log/nginx/error.log ]; then
    NGINX_ERRORS=$(sudo tail -100 /var/log/nginx/error.log | grep "$(date '+%Y/%m/%d')" | wc -l)
    if [ "$NGINX_ERRORS" -eq 0 ]; then
        echo -e "${GREEN}0${NC}"
    else
        echo -e "${YELLOW}$NGINX_ERRORS${NC}"
    fi
else
    echo -e "${YELLOW}Log file not found${NC}"
fi

echo
echo -e "${BLUE}=== VERSION INFO ===${NC}"
echo -e "${YELLOW}System:${NC} $(lsb_release -d | cut -f2)"
echo -e "${YELLOW}.NET:${NC} $(dotnet --version 2>/dev/null || echo 'Not found')"
echo -e "${YELLOW}Node.js:${NC} $(node --version 2>/dev/null || echo 'Not found')"
echo -e "${YELLOW}npm:${NC} $(npm --version 2>/dev/null || echo 'Not found')"
echo -e "${YELLOW}nginx:${NC} $(nginx -v 2>&1 | cut -d' ' -f3 | cut -d'/' -f2)"

echo
echo -e "${BLUE}========================================${NC}"

if [ $FAILED_CHECKS -eq 0 ]; then
    echo -e "${GREEN}   ✓ ALL CHECKS PASSED (${FAILED_CHECKS} failures)${NC}"
    echo -e "${GREEN}   RAG Suite is running correctly!${NC}"
    
    echo
    echo -e "${BLUE}🌐 Application URLs:${NC}"
    echo -e "   ${YELLOW}HTTP:${NC}  http://$DOMAIN_NAME"
    if netstat -tulpn | grep -q ':443 '; then
        echo -e "   ${YELLOW}HTTPS:${NC} https://$DOMAIN_NAME"
    fi
    echo -e "   ${YELLOW}API:${NC}   http://localhost:$API_PORT/health"
    
    exit 0
else
    echo -e "${RED}   ✗ SOME CHECKS FAILED ($FAILED_CHECKS failures)${NC}"
    echo -e "${RED}   Please check the failed components${NC}"
    
    echo
    echo -e "${BLUE}🔧 Suggested troubleshooting:${NC}"
    echo -e "   ${CYAN}sudo systemctl status rag-api${NC}     - Check API service"
    echo -e "   ${CYAN}sudo systemctl status nginx${NC}       - Check Nginx service"  
    echo -e "   ${CYAN}sudo journalctl -u rag-api -f${NC}     - View API logs"
    echo -e "   ${CYAN}sudo tail -f /var/log/nginx/error.log${NC} - View Nginx logs"
    echo -e "   ${CYAN}./fix-nodejs.sh${NC}                   - Fix Node.js issues"
    
    exit 1
fi
