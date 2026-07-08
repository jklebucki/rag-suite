#!/bin/bash

# RAG Suite Diagnosis Script
# Diagnozuje problemy i sugeruje rozwiązania

set -e

# Kolory dla outputu
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}      RAG Suite Diagnosis${NC}"
echo -e "${BLUE}========================================${NC}"
echo

# Sprawdź Ubuntu version
echo -e "${BLUE}=== SYSTEM INFO ===${NC}"
if command -v lsb_release &> /dev/null; then
    UBUNTU_VERSION=$(lsb_release -rs)
    UBUNTU_CODENAME=$(lsb_release -cs)
    echo -e "${YELLOW}Ubuntu:${NC} $UBUNTU_VERSION ($UBUNTU_CODENAME)"
else
    echo -e "${YELLOW}Ubuntu:${NC} Nieznana wersja"
    UBUNTU_VERSION="unknown"
fi

# Sprawdź libc6
LIBC_VERSION=$(dpkg -l | grep libc6 | awk '{print $3}' | head -1)
echo -e "${YELLOW}libc6:${NC} $LIBC_VERSION"

echo
echo -e "${BLUE}=== DOSTĘPNE NARZĘDZIA ===${NC}"

# Sprawdź podstawowe narzędzia
tools=("dotnet" "node" "npm" "nginx" "curl" "git")
available_tools=()
missing_tools=()

for tool in "${tools[@]}"; do
    if command -v $tool &> /dev/null; then
        version=""
        case $tool in
            "dotnet") version=$(dotnet --version 2>/dev/null) ;;
            "node") version=$(node --version 2>/dev/null) ;;
            "npm") version=$(npm --version 2>/dev/null) ;;
            "nginx") version=$(nginx -v 2>&1 | cut -d'/' -f2) ;;
            "git") version=$(git --version | cut -d' ' -f3) ;;
            "curl") version=$(curl --version | head -1 | cut -d' ' -f2) ;;
        esac
        echo -e "${GREEN}✓ $tool${NC} - $version"
        available_tools+=($tool)
    else
        echo -e "${RED}✗ $tool${NC} - Nie zainstalowany"
        missing_tools+=($tool)
    fi
done

echo
echo -e "${BLUE}=== DIAGNOZA PROBLEMÓW ===${NC}"

# Sprawdź czy jesteśmy w odpowiednim katalogu
if [ -f "production-setup.sh" ] && [ -f "deploy.sh" ]; then
    echo -e "${GREEN}✓ Znajdujesz się w katalogu RAG Suite${NC}"
    IN_RAG_DIR=true
else
    echo -e "${YELLOW}⚠ Nie znajdujesz się w katalogu RAG Suite${NC}"
    echo -e "  ${CYAN}Przejdź do: cd /var/www/rag-suite${NC}"
    IN_RAG_DIR=false
fi

# Sprawdź główne problemy
problems=()
solutions=()

# Problem z Node.js
if [[ ! " ${available_tools[@]} " =~ " node " ]]; then
    problems+=("Node.js nie jest zainstalowany")
    if [[ "$UBUNTU_VERSION" == "18.04" ]]; then
        solutions+=("sudo ./install-nodejs-alternative.sh  # Użyj alternatywnej instalacji dla Ubuntu 18.04")
    else
        solutions+=("sudo ./fix-nodejs.sh  # Napraw instalację Node.js")
    fi
elif command -v node &> /dev/null; then
    # Test czy Node.js działa
    if ! node -e "console.log('test')" &>/dev/null; then
        problems+=("Node.js jest zainstalowany ale nie działa")
        solutions+=("sudo ./fix-nodejs.sh  # Napraw Node.js")
    fi
fi

# Problem z .NET
if [[ ! " ${available_tools[@]} " =~ " dotnet " ]]; then
    problems+=(".NET SDK nie jest zainstalowany")
    solutions+=("sudo ./production-setup.sh  # Uruchom pełną konfigurację")
elif command -v dotnet &> /dev/null; then
    # Sprawdź czy .NET 10 jest dostępny
    if ! dotnet --list-sdks 2>/dev/null | grep -q "10\.0\."; then
        problems+=(".NET 10 SDK nie jest zainstalowany (wymagany dla aplikacji)")
        current_dotnet=$(dotnet --version 2>/dev/null || echo "nieznana")
        solutions+=("sudo ./install-dotnet10.sh  # Zainstaluj .NET 10 SDK (obecna wersja: $current_dotnet)")
    fi
fi

# Problem z nginx
if [[ ! " ${available_tools[@]} " =~ " nginx " ]]; then
    problems+=("Nginx nie jest zainstalowany")
    solutions+=("sudo ./production-setup.sh  # Uruchom pełną konfigurację")
elif ! systemctl is-active --quiet nginx 2>/dev/null; then
    problems+=("Nginx nie działa")
    solutions+=("sudo systemctl start nginx  # Uruchom Nginx")
fi

# Sprawdź czy serwis API działa
if systemctl is-active --quiet rag-api 2>/dev/null; then
    echo -e "${GREEN}✓ Serwis rag-api działa${NC}"
else
    problems+=("Serwis rag-api nie działa")
    solutions+=("sudo systemctl start rag-api  # Uruchom API")
fi

# Sprawdź czy aplikacja została zbudowana
if [ -d "/var/www/rag-suite/build" ]; then
    echo -e "${GREEN}✓ Aplikacja została zbudowana${NC}"
else
    problems+=("Aplikacja nie została zbudowana")
    solutions+=("sudo ./deploy.sh  # Zbuduj i wdróż aplikację")
fi

echo
if [ ${#problems[@]} -eq 0 ]; then
    echo -e "${GREEN}🎉 Nie wykryto problemów!${NC}"
    echo -e "${BLUE}Sprawdź działanie aplikacji:${NC}"
    echo -e "  ${CYAN}./health-check.sh${NC}"
else
    echo -e "${RED}🔍 Wykryto ${#problems[@]} problem(ów):${NC}"
    echo
    
    for i in "${!problems[@]}"; do
        echo -e "${RED}$((i+1)). ${problems[i]}${NC}"
        echo -e "   ${CYAN}Rozwiązanie: ${solutions[i]}${NC}"
        echo
    done
fi

echo -e "${BLUE}=== KOLEJNE KROKI ===${NC}"

if [[ "$UBUNTU_VERSION" == "18.04" ]] && [[ ! " ${available_tools[@]} " =~ " node " ]]; then
    echo -e "${YELLOW}UWAGA: Ubuntu 18.04 ma problemy z nowymi wersjami Node.js${NC}"
    echo -e "${CYAN}1. sudo ./install-nodejs-alternative.sh${NC}  # Zainstaluj Node.js przez Snap"
    echo -e "${CYAN}2. sudo ./deploy.sh${NC}                      # Zbuduj aplikację"
    echo -e "${CYAN}3. sudo ./ssl-setup.sh${NC}                   # Skonfiguruj HTTPS (opcjonalne)"
    
elif [ ${#problems[@]} -gt 0 ]; then
    echo -e "${CYAN}1. Rozwiąż problemy powyżej${NC}"
    echo -e "${CYAN}2. ./health-check.sh${NC}                     # Sprawdź czy wszystko działa"
    
else
    echo -e "${CYAN}1. ./health-check.sh${NC}                     # Sprawdź działanie"
    echo -e "${CYAN}2. curl http://localhost${NC}                 # Test aplikacji"
    if netstat -tulpn 2>/dev/null | grep -q ':443 '; then
        echo -e "${CYAN}3. curl https://localhost${NC}                # Test HTTPS"
    fi
fi

echo
echo -e "${BLUE}📖 Pełna dokumentacja:${NC} PRODUCTION-DEPLOYMENT.md"
echo -e "${BLUE}🚀 Szybki start:${NC} ADMIN-QUICKSTART.md"
echo
