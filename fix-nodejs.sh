#!/bin/bash

# RAG Suite Node.js Fix Script
# Naprawia problemy z instalacją Node.js na różnych wersjach Ubuntu

set -e

# Kolory dla outputu
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}    RAG Suite Node.js Fix Script${NC}"
echo -e "${BLUE}========================================${NC}"

# Sprawdź czy skrypt jest uruchamiany jako root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}Ten skrypt musi być uruchomiony jako root (użyj sudo)${NC}"
   exit 1
fi

echo -e "${BLUE}[1/4] Sprawdzanie systemu...${NC}"

# Sprawdź wersję Ubuntu
if command -v lsb_release &> /dev/null; then
    UBUNTU_VERSION=$(lsb_release -rs)
    UBUNTU_CODENAME=$(lsb_release -cs)
    echo -e "${YELLOW}Wykryto Ubuntu $UBUNTU_VERSION ($UBUNTU_CODENAME)${NC}"
else
    echo -e "${YELLOW}Nie można wykryć wersji Ubuntu${NC}"
    UBUNTU_VERSION="unknown"
fi

# Sprawdź aktualną wersję libc6
LIBC_VERSION=$(dpkg -l | grep libc6 | awk '{print $3}' | head -1)
echo -e "${YELLOW}Aktualna wersja libc6: $LIBC_VERSION${NC}"

echo -e "${BLUE}[2/4] Usuwanie konfliktowych źródeł Node.js...${NC}"

# Usuń istniejące repozytoria NodeSource
rm -f /etc/apt/sources.list.d/nodesource.list
rm -f /usr/share/keyrings/nodesource.gpg

# Usuń Node.js jeśli jest zainstalowany z błędnego repozytorium
if command -v node &> /dev/null; then
    CURRENT_NODE_VERSION=$(node --version)
    echo -e "${YELLOW}Obecna wersja Node.js: $CURRENT_NODE_VERSION${NC}"
    
    # Sprawdź czy Node.js działa poprawnie
    if node -e "console.log('test')" &> /dev/null; then
        echo -e "${GREEN}✓ Node.js działa poprawnie, nie ma potrzeby reinstalacji${NC}"
        exit 0
    else
        echo -e "${YELLOW}Node.js nie działa poprawnie - reinstalacja...${NC}"
        apt remove -y nodejs npm
    fi
fi

echo -e "${BLUE}[3/4] Instalacja odpowiedniej wersji Node.js...${NC}"

# Wybierz odpowiednią wersję Node.js na podstawie Ubuntu
if [[ "$UBUNTU_VERSION" == "18.04" ]]; then
    echo -e "${BLUE}Ubuntu 18.04 - próba instalacji Node.js 16 LTS${NC}"
    
    # Próba instalacji Node.js 16
    curl -fsSL https://deb.nodesource.com/setup_16.x | bash -
    
    if apt install -y nodejs; then
        echo -e "${GREEN}✓ Node.js 16 zainstalowany pomyślnie${NC}"
    else
        echo -e "${YELLOW}Node.js 16 niekompatybilny - próba alternatywnej instalacji przez Snap${NC}"
        
        # Zainstaluj snapd jeśli nie ma
        if ! command -v snap &> /dev/null; then
            apt update
            apt install -y snapd
            systemctl enable snapd
            systemctl start snapd
            sleep 5
        fi
        
        # Zainstaluj Node.js przez snap
        snap install node --classic --channel=16/stable
        
        # Utworz linki symboliczne
        ln -sf /snap/bin/node /usr/local/bin/node
        ln -sf /snap/bin/npm /usr/local/bin/npm
        
        echo -e "${GREEN}✓ Node.js zainstalowany przez Snap${NC}"
    fi
    
elif [[ "$UBUNTU_VERSION" == "20.04" ]]; then
    echo -e "${BLUE}Ubuntu 20.04 - instalacja Node.js 18 LTS${NC}"
    curl -fsSL https://deb.nodesource.com/setup_18.x | bash -
    apt install -y nodejs
    
elif [[ "$UBUNTU_VERSION" == "22.04" ]] || [[ "$UBUNTU_VERSION" == "24.04" ]]; then
    echo -e "${BLUE}Ubuntu $UBUNTU_VERSION - instalacja Node.js 20 LTS${NC}"
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
    apt install -y nodejs
    
else
    echo -e "${YELLOW}Nieznana/nieobsługiwana wersja Ubuntu - próba z Node.js 16 LTS (najbardziej kompatybilna)${NC}"
    curl -fsSL https://deb.nodesource.com/setup_16.x | bash -
    
    if ! apt install -y nodejs; then
        echo -e "${YELLOW}NodeSource niekompatybilny - próba instalacji przez Snap${NC}"
        
        # Snap fallback
        if ! command -v snap &> /dev/null; then
            apt update
            apt install -y snapd
            systemctl enable snapd
            systemctl start snapd
            sleep 5
        fi
        
        snap install node --classic --channel=16/stable
        ln -sf /snap/bin/node /usr/local/bin/node
        ln -sf /snap/bin/npm /usr/local/bin/npm
    fi
fi

echo -e "${BLUE}[4/4] Weryfikacja instalacji...${NC}"

# Sprawdź czy Node.js działa
if command -v node &> /dev/null; then
    NODE_VERSION=$(node --version)
    echo -e "${GREEN}✓ Node.js $NODE_VERSION zainstalowany pomyślnie${NC}"
else
    echo -e "${RED}✗ Błąd instalacji Node.js${NC}"
    exit 1
fi

# Sprawdź czy npm działa
if command -v npm &> /dev/null; then
    NPM_VERSION=$(npm --version)
    echo -e "${GREEN}✓ npm $NPM_VERSION dostępny${NC}"
else
    echo -e "${RED}✗ npm nie jest dostępny${NC}"
    exit 1
fi

# Test funkcjonalności
echo -e "${YELLOW}Test funkcjonalności Node.js...${NC}"
if node -e "console.log('Node.js działa poprawnie!')"; then
    echo -e "${GREEN}✓ Node.js przeszedł test funkcjonalności${NC}"
else
    echo -e "${RED}✗ Node.js nie przeszedł testu funkcjonalności${NC}"
    exit 1
fi

# Test npm
echo -e "${YELLOW}Test npm...${NC}"
if npm --version &> /dev/null; then
    echo -e "${GREEN}✓ npm działa poprawnie${NC}"
else
    echo -e "${RED}✗ npm nie działa poprawnie${NC}"
    exit 1
fi

echo -e "\n${GREEN}═══════════════════════════════════════${NC}"
echo -e "${GREEN}     NODE.JS NAPRAWIONY POMYŚLNIE       ${NC}"
echo -e "${GREEN}═══════════════════════════════════════${NC}"
echo
echo -e "${BLUE}📋 Informacje o instalacji:${NC}"
echo -e "   Ubuntu: $UBUNTU_VERSION"
echo -e "   Node.js: $(node --version)"
echo -e "   npm: $(npm --version)"
echo -e "   libc6: $LIBC_VERSION"
echo
echo -e "${BLUE}🚀 Następne kroki:${NC}"
echo -e "   ${CYAN}cd /var/www/rag-suite${NC}"
echo -e "   ${CYAN}sudo ./deploy.sh${NC}"
echo
