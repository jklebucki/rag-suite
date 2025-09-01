#!/bin/bash

# RAG Suite - Instalacja .NET 8 SDK
# Bezpieczna instalacja .NET 8 z autodetekcją Ubuntu
# Nie usuwa istniejących wersji .NET

set -e

# Kolory dla outputu
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}    .NET 8 SDK Installation${NC}"
echo -e "${BLUE}========================================${NC}"
echo

# Funkcja do sprawdzania czy komenda się powiodła
check_command() {
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ $1${NC}"
    else
        echo -e "${RED}✗ $1${NC}"
        exit 1
    fi
}

# Sprawdź czy jesteśmy root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}Ten skrypt musi być uruchomiony jako root (sudo)${NC}"
   exit 1
fi

# Wykryj wersję Ubuntu
echo -e "${BLUE}=== SPRAWDZENIE SYSTEMU ===${NC}"
if command -v lsb_release &> /dev/null; then
    UBUNTU_VERSION=$(lsb_release -rs)
    UBUNTU_CODENAME=$(lsb_release -cs)
    echo -e "${YELLOW}Ubuntu:${NC} $UBUNTU_VERSION ($UBUNTU_CODENAME)"
else
    echo -e "${RED}Nie można wykryć wersji Ubuntu${NC}"
    exit 1
fi

# Sprawdź istniejące instalacje .NET
echo
echo -e "${BLUE}=== ISTNIEJĄCE INSTALACJE .NET ===${NC}"
if command -v dotnet &> /dev/null; then
    echo -e "${YELLOW}Wykryto istniejące instalacje .NET:${NC}"
    dotnet --list-sdks 2>/dev/null || echo "Brak informacji o SDK"
    dotnet --list-runtimes 2>/dev/null || echo "Brak informacji o Runtime"
    echo
    
    # Sprawdź czy .NET 8 już jest
    if dotnet --list-sdks 2>/dev/null | grep -q "8\.0\."; then
        echo -e "${GREEN}✓ .NET 8 SDK już jest zainstalowany!${NC}"
        dotnet --list-sdks | grep "8\.0\."
        echo -e "${CYAN}Sprawdź czy aplikacja teraz się buduje:${NC}"
        echo -e "  cd /var/www/rag-suite"
        echo -e "  dotnet build src/RAG.Orchestrator.Api/RAG.Orchestrator.Api.csproj"
        exit 0
    fi
else
    echo -e "${YELLOW}Nie wykryto żadnych instalacji .NET${NC}"
fi

echo -e "${BLUE}=== INSTALACJA .NET 8 SDK ===${NC}"

# Konfiguracja repozytorium Microsoft na podstawie wersji Ubuntu
case $UBUNTU_VERSION in
    "18.04")
        echo -e "${YELLOW}Konfiguracja dla Ubuntu 18.04 (bionic)...${NC}"
        
        # Instaluj dependencies
        apt-get update
        apt-get install -y wget apt-transport-https software-properties-common gpg
        
        # Pobierz Microsoft signing key i repozytorium - poprawna metoda dla Ubuntu 18.04
        wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg
        mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/
        chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg
        
        # Dodaj Microsoft repository - używamy bezpośrednio prod.list
        wget -q https://packages.microsoft.com/config/ubuntu/18.04/prod.list
        mv prod.list /etc/apt/sources.list.d/microsoft-prod.list
        chown root:root /etc/apt/sources.list.d/microsoft-prod.list
        
        check_command "Dodano repozytorium Microsoft dla Ubuntu 18.04"
        ;;
        
    "20.04")
        echo -e "${YELLOW}Konfiguracja dla Ubuntu 20.04 (focal)...${NC}"
        
        # Instaluj dependencies
        apt-get update
        apt-get install -y wget apt-transport-https software-properties-common gpg
        
        # Podobna konfiguracja jak dla 18.04
        wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg
        mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/
        chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg
        
        wget -q https://packages.microsoft.com/config/ubuntu/20.04/prod.list
        mv prod.list /etc/apt/sources.list.d/microsoft-prod.list
        chown root:root /etc/apt/sources.list.d/microsoft-prod.list
        
        check_command "Dodano repozytorium Microsoft dla Ubuntu 20.04"
        ;;
        
    "22.04"|"24.04")
        echo -e "${YELLOW}Konfiguracja dla Ubuntu $UBUNTU_VERSION...${NC}"
        
        # Instaluj dependencies
        apt-get update
        apt-get install -y wget apt-transport-https software-properties-common gpg
        
        # Dla nowszych wersji Ubuntu - Microsoft już nie wspiera 24.04+, ale 22.04 jeszcze tak
        if [[ "$UBUNTU_VERSION" == "22.04" ]]; then
            # Ubuntu 22.04 może używać Microsoft repo
            wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg
            mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/
            chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg
            
            wget -q https://packages.microsoft.com/config/ubuntu/22.04/prod.list
            mv prod.list /etc/apt/sources.list.d/microsoft-prod.list
            chown root:root /etc/apt/sources.list.d/microsoft-prod.list
            
            check_command "Dodano repozytorium Microsoft dla Ubuntu 22.04"
        else
            # Ubuntu 24.04+ - próbuj Ubuntu backports lub wbudowany feed
            echo -e "${CYAN}Ubuntu 24.04+ - próba instalacji z Ubuntu feeds...${NC}"
            
            # Sprawdź czy dotnet-sdk-8.0 jest dostępny w Ubuntu feeds
            if apt-cache search dotnet-sdk-8.0 | grep -q "dotnet-sdk-8.0"; then
                echo -e "${GREEN}✓ .NET 8 SDK dostępny w Ubuntu feeds${NC}"
            else
                echo -e "${YELLOW}Dodawanie Ubuntu .NET backports repository...${NC}"
                apt-get install -y software-properties-common
                add-apt-repository -y ppa:dotnet/backports
                apt-get update
            fi
        fi
        ;;
        
    *)
        echo -e "${YELLOW}Nieznana wersja Ubuntu ($UBUNTU_VERSION), próbuję uniwersalną metodę...${NC}"
        
        # Uniwersalna metoda przez .NET install script
        echo -e "${CYAN}Pobieranie .NET install script...${NC}"
        curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version latest --channel 8.0 --install-dir /usr/share/dotnet
        
        # Dodaj do PATH jeśli nie ma - bezpieczne dodanie
        if ! echo $PATH | grep -q "/usr/share/dotnet"; then
            # Sprawdź czy /etc/environment istnieje i ma PATH
            if [ -f "/etc/environment" ] && grep -q "^PATH=" /etc/environment; then
                # Jeśli istnieje PATH w /etc/environment, dodaj do niego
                if ! grep -q "/usr/share/dotnet" /etc/environment; then
                    sed -i 's|PATH="\(.*\)"|PATH="\1:/usr/share/dotnet"|' /etc/environment
                fi
            else
                # Jeśli nie ma PATH w /etc/environment, utwórz bezpieczny
                echo 'PATH="/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/usr/games:/usr/local/games:/usr/share/dotnet"' >> /etc/environment
            fi
            export PATH=$PATH:/usr/share/dotnet
        fi
        
        # Sprawdź instalację
        if /usr/share/dotnet/dotnet --version &>/dev/null; then
            echo -e "${GREEN}✓ .NET 8 zainstalowany przez install script${NC}"
            /usr/share/dotnet/dotnet --version
            exit 0
        else
            echo -e "${RED}✗ Instalacja przez script nie powiodła się${NC}"
            exit 1
        fi
        ;;
esac

# Aktualizuj listę pakietów
echo -e "${CYAN}Aktualizacja listy pakietów...${NC}"
apt-get update
check_command "Aktualizacja listy pakietów"

# Zainstaluj .NET 8 SDK
echo -e "${CYAN}Instalacja .NET 8 SDK...${NC}"

# Dla Ubuntu 18.04 i 20.04 - dodaj fallback w przypadku problemów z Microsoft repo
if [[ "$UBUNTU_VERSION" == "18.04" ]] || [[ "$UBUNTU_VERSION" == "20.04" ]]; then
    if ! apt-get install -y dotnet-sdk-8.0; then
        echo -e "${YELLOW}Instalacja przez Microsoft repository nie powiodła się. Próbuję .NET install script...${NC}"
        
        # Fallback - manual install script
        curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version latest --channel 8.0 --install-dir /usr/share/dotnet
        
        # Dodaj do PATH globalnie - bezpieczne dodanie
        if ! echo $PATH | grep -q "/usr/share/dotnet"; then
            # Sprawdź czy /etc/environment istnieje i ma PATH
            if [ -f "/etc/environment" ] && grep -q "^PATH=" /etc/environment; then
                # Jeśli istnieje PATH w /etc/environment, dodaj do niego dotnet
                if ! grep -q "/usr/share/dotnet" /etc/environment; then
                    sed -i 's|PATH="\(.*\)"|PATH="\1:/usr/share/dotnet"|' /etc/environment
                fi
                # Dodaj DOTNET_ROOT jeśli go nie ma
                if ! grep -q "DOTNET_ROOT" /etc/environment; then
                    echo 'DOTNET_ROOT="/usr/share/dotnet"' >> /etc/environment
                fi
            else
                # Jeśli nie ma PATH w /etc/environment, utwórz bezpieczny
                echo 'PATH="/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin:/usr/games:/usr/local/games:/usr/share/dotnet"' >> /etc/environment
                echo 'DOTNET_ROOT="/usr/share/dotnet"' >> /etc/environment
            fi
            export PATH=$PATH:/usr/share/dotnet
            export DOTNET_ROOT=/usr/share/dotnet
        fi
        
        # Stwórz symlink dla dotnet command
        ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
        
        # Sprawdź czy działa
        if dotnet --version &>/dev/null; then
            echo -e "${GREEN}✓ .NET 8 zainstalowany przez install script${NC}"
        else
            echo -e "${RED}✗ Instalacja nie powiodła się${NC}"
            exit 1
        fi
    else
        check_command "Instalacja .NET 8 SDK przez Microsoft repository"
    fi
else
    # Dla nowszych wersji Ubuntu
    apt-get install -y dotnet-sdk-8.0
    check_command "Instalacja .NET 8 SDK"
fi

# Sprawdź instalację
echo
echo -e "${BLUE}=== WERYFIKACJA INSTALACJI ===${NC}"
if command -v dotnet &> /dev/null; then
    echo -e "${GREEN}✓ .NET jest dostępny${NC}"
    
    echo -e "${YELLOW}Wszystkie zainstalowane SDK:${NC}"
    dotnet --list-sdks
    
    echo -e "${YELLOW}Wszystkie zainstalowane Runtime:${NC}"
    dotnet --list-runtimes
    
    # Sprawdź czy .NET 8 jest dostępny
    if dotnet --list-sdks | grep -q "8\.0\."; then
        echo -e "${GREEN}✓ .NET 8 SDK jest dostępny!${NC}"
        
        # Test kompilacji
        echo
        echo -e "${BLUE}=== TEST KOMPILACJI ===${NC}"
        if [ -f "/var/www/rag-suite/src/RAG.Orchestrator.Api/RAG.Orchestrator.Api.csproj" ]; then
            echo -e "${CYAN}Testowanie kompilacji RAG.Orchestrator.Api...${NC}"
            cd /var/www/rag-suite
            
            if dotnet build src/RAG.Orchestrator.Api/RAG.Orchestrator.Api.csproj --verbosity quiet; then
                echo -e "${GREEN}✓ Kompilacja zakończona sukcesem!${NC}"
                echo -e "${BLUE}Możesz teraz uruchomić pełny deployment:${NC}"
                echo -e "  ${CYAN}sudo ./deploy.sh${NC}"
            else
                echo -e "${RED}✗ Kompilacja nie powiodła się${NC}"
                echo -e "${YELLOW}Sprawdź logi błędów powyżej${NC}"
            fi
        else
            echo -e "${YELLOW}Projekt nie znaleziony w /var/www/rag-suite${NC}"
            echo -e "${CYAN}Uruchom kompilację ręcznie:${NC}"
            echo -e "  cd /path/to/rag-suite"
            echo -e "  dotnet build src/RAG.Orchestrator.Api/RAG.Orchestrator.Api.csproj"
        fi
    else
        echo -e "${RED}✗ .NET 8 SDK nie został zainstalowany${NC}"
        exit 1
    fi
else
    echo -e "${RED}✗ .NET nie jest dostępny po instalacji${NC}"
    exit 1
fi

echo
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}    .NET 8 SDK został zainstalowany!${NC}"
echo -e "${GREEN}========================================${NC}"
echo
echo -e "${BLUE}Następne kroki:${NC}"
echo -e "1. ${CYAN}cd /var/www/rag-suite${NC}"
echo -e "2. ${CYAN}sudo ./deploy.sh${NC}              # Pełny deployment"
echo -e "3. ${CYAN}./diagnose.sh${NC}                 # Sprawdź status"
echo
