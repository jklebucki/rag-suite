#!/bin/bash

# RAG Suite Node.js Alternative Install Script  
# Instaluje Node.js używając alternatywnych metod dla starszych systemów Ubuntu

set -e

# Kolory dla outputu
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}   RAG Suite Node.js Alternative Install${NC}"
echo -e "${BLUE}========================================${NC}"

# Sprawdź czy skrypt jest uruchamiany jako root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}Ten skrypt musi być uruchomiony jako root (użyj sudo)${NC}"
   exit 1
fi

# Sprawdź wersję Ubuntu
if command -v lsb_release &> /dev/null; then
    UBUNTU_VERSION=$(lsb_release -rs)
    UBUNTU_CODENAME=$(lsb_release -cs)
    echo -e "${YELLOW}Wykryto Ubuntu $UBUNTU_VERSION ($UBUNTU_CODENAME)${NC}"
else
    echo -e "${YELLOW}Nie można wykryć wersji Ubuntu${NC}"
    UBUNTU_VERSION="unknown"
fi

echo -e "${BLUE}[1/3] Czyszczenie istniejących instalacji Node.js...${NC}"

# Usuń Node.js z apt
apt remove -y nodejs npm 2>/dev/null || true
apt autoremove -y 2>/dev/null || true

# Usuń repozytoria NodeSource
rm -f /etc/apt/sources.list.d/nodesource.list 2>/dev/null || true
rm -f /usr/share/keyrings/nodesource.gpg 2>/dev/null || true

# Usuń cache
apt update

echo -e "${BLUE}[2/3] Instalacja Node.js poprzez Snap...${NC}"

# Zainstaluj snapd jeśli nie ma
if ! command -v snap &> /dev/null; then
    echo -e "${YELLOW}Instalacja snapd...${NC}"
    apt update
    apt install -y snapd
    systemctl enable snapd
    systemctl start snapd
    
    # Poczekaj na uruchomienie snapd
    sleep 5
fi

# Zainstaluj Node.js przez snap (wersja 16 jest dostępna jako classic)
echo -e "${YELLOW}Instalacja Node.js 16 przez Snap...${NC}"
snap install node --classic --channel=16/stable

# Sprawdź czy snap Node.js działa
if /snap/bin/node --version &> /dev/null; then
    echo -e "${GREEN}✓ Node.js zainstalowany przez Snap: $(/snap/bin/node --version)${NC}"
    
    # Utworz linki symboliczne
    ln -sf /snap/bin/node /usr/local/bin/node
    ln -sf /snap/bin/npm /usr/local/bin/npm
    
    echo -e "${GREEN}✓ Utworzono linki symboliczne w /usr/local/bin${NC}"
else
    echo -e "${RED}✗ Błąd instalacji Node.js przez Snap${NC}"
    
    echo -e "${BLUE}[3/3] Próba instalacji z Ubuntu repository...${NC}"
    
    # Fallback - użyj wersji z standardowego repozytorium Ubuntu
    apt update
    apt install -y nodejs npm
    
    if command -v node &> /dev/null; then
        echo -e "${GREEN}✓ Node.js zainstalowany z Ubuntu repo: $(node --version)${NC}"
    else
        echo -e "${RED}✗ Wszystkie metody instalacji Node.js zawiodły${NC}"
        exit 1
    fi
fi

echo -e "${BLUE}[3/3] Weryfikacja instalacji...${NC}"

# Test Node.js
if command -v node &> /dev/null; then
    NODE_VERSION=$(node --version)
    echo -e "${GREEN}✓ Node.js $NODE_VERSION działa poprawnie${NC}"
else
    echo -e "${RED}✗ Node.js nie jest dostępny${NC}"
    exit 1
fi

# Test npm
if command -v npm &> /dev/null; then
    NPM_VERSION=$(npm --version)
    echo -e "${GREEN}✓ npm $NPM_VERSION działa poprawnie${NC}"
else
    echo -e "${RED}✗ npm nie jest dostępny${NC}"
    exit 1
fi

# Test funkcjonalności
echo -e "${YELLOW}Test funkcjonalności...${NC}"
if node -e "console.log('Node.js działa!')"; then
    echo -e "${GREEN}✓ Test funkcjonalności przeszedł pomyślnie${NC}"
else
    echo -e "${RED}✗ Test funkcjonalności nie powiódł się${NC}"
    exit 1
fi

echo -e "\n${GREEN}═══════════════════════════════════════${NC}"
echo -e "${GREEN}   NODE.JS ZAINSTALOWANY POMYŚLNIE      ${NC}"
echo -e "${GREEN}═══════════════════════════════════════${NC}"
echo
echo -e "${BLUE}📋 Informacje o instalacji:${NC}"
echo -e "   Ubuntu: $UBUNTU_VERSION"
echo -e "   Node.js: $(node --version)"
echo -e "   npm: $(npm --version)"
echo -e "   Metoda: Snap + symbolic links"
echo
echo -e "${BLUE}📍 Lokalizacje:${NC}"
echo -e "   Node.js binary: $(which node)"
echo -e "   npm binary: $(which npm)"
echo
echo -e "${BLUE}🚀 Następne kroki:${NC}"
echo -e "   ${CYAN}cd /var/www/rag-suite${NC}"
echo -e "   ${CYAN}sudo ./deploy.sh${NC}"
echo
