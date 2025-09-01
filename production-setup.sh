#!/bin/bash

# RAG Suite Production Setup Script
# Installs and configures everything needed for production deployment

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
APP_NAME="rag-suite"
APP_USER="www-data"
APP_DIR="/var/www/rag-suite"
DOMAIN_NAME="${DOMAIN_NAME:-asystent.ad.citronex.pl}"
API_PORT="5000"

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}    RAG Suite Production Setup${NC}"
echo -e "${BLUE}========================================${NC}"
echo -e "${YELLOW}Domain: ${DOMAIN_NAME}${NC}"
echo -e "${YELLOW}App Directory: ${APP_DIR}${NC}"
echo -e "${YELLOW}API Port: ${API_PORT}${NC}"
echo ""

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}Ten skrypt musi być uruchomiony jako root (użyj sudo)${NC}"
   exit 1
fi

echo -e "${BLUE}[1/7] Instalacja .NET 8 SDK...${NC}"

# Use dedicated .NET 8 installation script
if [ -f "./install-dotnet8.sh" ]; then
    echo -e "${YELLOW}Uruchamianie install-dotnet8.sh...${NC}"
    ./install-dotnet8.sh
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ .NET 8 zainstalowany pomyślnie${NC}"
    else
        echo -e "${RED}✗ Błąd instalacji .NET 8${NC}"
        exit 1
    fi
else
    echo -e "${RED}✗ Brak pliku install-dotnet8.sh${NC}"
    exit 1
fi

echo -e "${BLUE}[2/7] Instalacja Node.js i NPM...${NC}"

# Check if Node.js is already installed
if command -v node &> /dev/null && command -v npm &> /dev/null; then
    # Sprawdź czy Node.js jest z snap
    if command -v snap >/dev/null 2>&1 && snap list node >/dev/null 2>&1; then
        echo -e "${YELLOW}⚠ Node.js $(node --version) zainstalowany przez snap${NC}"
        echo -e "${YELLOW}Snap ma ograniczenia dla katalogów poza /home${NC}"
        echo -e "${YELLOW}Deploy script użyje obejścia przez /tmp${NC}"
    else
        echo -e "${GREEN}✓ Node.js $(node --version) i NPM $(npm --version) już zainstalowane${NC}"
    fi
else
    echo -e "${YELLOW}Próba instalacji Node.js...${NC}"
    
    # Install Node.js (may fail on older systems)
    curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
    
    if apt-get install -y nodejs; then
        echo -e "${GREEN}✓ Node.js zainstalowany pomyślnie${NC}"
    else
        echo -e "${YELLOW}⚠ Błąd instalacji Node.js z NodeSource repository${NC}"
        echo -e "${YELLOW}Próba instalacji z domyślnych repozytoriów Ubuntu...${NC}"
        
        # Try installing from default Ubuntu repositories
        if apt-get install -y nodejs npm; then
            echo -e "${GREEN}✓ Node.js zainstalowany z domyślnych repozytoriów${NC}"
            
            # Sprawdź czy zainstalował się przez snap
            if command -v snap >/dev/null 2>&1 && snap list node >/dev/null 2>&1; then
                echo -e "${YELLOW}⚠ Node.js zainstalowany przez snap - może mieć ograniczenia katalogów${NC}"
            fi
        else
            echo -e "${YELLOW}⚠ Nie udało się zainstalować Node.js${NC}"
            echo -e "${YELLOW}Możesz kontynuować bez Node.js lub zainstalować go ręcznie później${NC}"
            echo -e "${YELLOW}Komenda: sudo apt-get install nodejs npm${NC}"
        fi
    fi
    
    # Verify installation (non-blocking)
    if command -v node &> /dev/null && command -v npm &> /dev/null; then
        echo -e "${GREEN}✓ Node.js $(node --version) i NPM $(npm --version) dostępne${NC}"
    else
        echo -e "${YELLOW}⚠ Node.js nie jest dostępny - niektóre funkcje mogą być ograniczone${NC}"
    fi
fi

echo -e "${BLUE}[3/7] Instalacja i konfiguracja Nginx...${NC}"

# Install nginx
apt-get update
apt-get install -y nginx

# Enable and start nginx
systemctl enable nginx
systemctl start nginx

echo -e "${GREEN}✓ Nginx zainstalowany${NC}"

echo -e "${BLUE}[4/7] Tworzenie systemd service dla API...${NC}"

# Create user if doesn't exist
if ! id "$APP_USER" &>/dev/null; then
    useradd --system --no-create-home --shell /bin/false $APP_USER
fi

# Create systemd service for the API
cat > /etc/systemd/system/rag-api.service << EOF
[Unit]
Description=RAG Suite API
After=network.target

[Service]
Type=simple
User=$APP_USER
Group=$APP_USER
WorkingDirectory=$APP_DIR/build/api
ExecStart=/usr/bin/dotnet RAG.Orchestrator.Api.dll
Restart=always
RestartSec=10
TimeoutStartSec=60
TimeoutStopSec=30
SyslogIdentifier=rag-api
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:$API_PORT
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

# Security settings
NoNewPrivileges=yes
PrivateTmp=yes
ProtectSystem=strict
ReadWritePaths=$APP_DIR
ProtectHome=yes

[Install]
WantedBy=multi-user.target
EOF

echo -e "${GREEN}✓ Systemd service utworzony${NC}"

echo -e "${BLUE}[5/7] Konfiguracja Nginx...${NC}"

# Use dedicated nginx setup script
if [ -f "./nginx-setup.sh" ]; then
    echo -e "${YELLOW}Uruchamianie nginx-setup.sh...${NC}"
    ./nginx-setup.sh
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ Konfiguracja Nginx zakończona pomyślnie${NC}"
    else
        echo -e "${RED}✗ Błąd konfiguracji Nginx${NC}"
        exit 1
    fi
else
    echo -e "${RED}✗ Brak pliku nginx-setup.sh${NC}"
    exit 1
fi

echo -e "${BLUE}[6/7] Tworzenie katalogów i ustawianie uprawnień...${NC}"

# Create build directories
mkdir -p $APP_DIR/build/api
mkdir -p $APP_DIR/build/web
mkdir -p /var/log/rag-suite

# Set ownership
chown -R $APP_USER:$APP_USER $APP_DIR
chown -R $APP_USER:$APP_USER /var/log/rag-suite

# Set permissions
chmod -R 755 $APP_DIR
chmod -R 644 $APP_DIR/build/web/* 2>/dev/null || true

echo -e "${GREEN}✓ Katalogi i uprawnienia skonfigurowane${NC}"

echo -e "${BLUE}[7/7] Konfiguracja firewall i finalizacja...${NC}"

# Configure UFW firewall
if command -v ufw >/dev/null 2>&1; then
    echo -e "${YELLOW}Konfiguracja firewall UFW...${NC}"
    
    # Enable UFW if not enabled
    if ! ufw status | grep -q "Status: active"; then
        echo -e "${YELLOW}Włączanie UFW...${NC}"
        ufw --force enable
    fi
    
    # Allow SSH (important!)
    ufw allow ssh
    
    # Allow HTTP and HTTPS
    ufw allow 80/tcp
    ufw allow 443/tcp
    
    # Allow API port for direct access (optional - for testing)
    ufw allow $API_PORT/tcp comment "RAG API"
    
    echo -e "${GREEN}✓ Firewall skonfigurowany${NC}"
else
    echo -e "${YELLOW}⚠ UFW nie jest zainstalowany - pomiń konfigurację firewall${NC}"
fi

# Reload systemd
systemctl daemon-reload

# Enable services (but don't start yet)
systemctl enable rag-api

# Check installed versions
echo -e "${YELLOW}Zainstalowane wersje:${NC}"
echo "- .NET: $(dotnet --version)"
if command -v node &> /dev/null; then
    echo "- Node.js: $(node --version)"
else
    echo "- Node.js: nie zainstalowany"
fi
if command -v npm &> /dev/null; then
    echo "- NPM: $(npm --version)"
else
    echo "- NPM: nie zainstalowany"
fi
echo "- Nginx: $(nginx -v 2>&1 | cut -d: -f2 | cut -d/ -f2)"

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}    Setup zakończony pomyślnie!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}Co dalej:${NC}"
echo "1. Zbuduj aplikacje: sudo ./deploy.sh"
echo "2. Uruchom serwisy: sudo systemctl start rag-api"
echo ""
echo -e "${YELLOW}Dostęp do API:${NC}"
echo "- Przez nginx: http://$DOMAIN_NAME/api/"
echo "- Bezpośrednio: http://$DOMAIN_NAME:$API_PORT/"
echo "- Health check: http://$DOMAIN_NAME:$API_PORT/health"
echo ""
echo -e "${YELLOW}Konfiguracja SSL (opcjonalnie):${NC}"
echo "sudo certbot --nginx -d $DOMAIN_NAME"
echo ""
echo -e "${YELLOW}Użyteczne komendy:${NC}"
echo "- Status API: sudo systemctl status rag-api"
echo "- Logi API: sudo journalctl -fu rag-api"
echo "- Status Nginx: sudo systemctl status nginx"
echo "- Logi Nginx: sudo tail -f /var/log/nginx/error.log"
echo "- Test konfiguracji: sudo nginx -t"
echo "- Reload Nginx: sudo systemctl reload nginx"
echo ""

# Check if application is already built
if [ ! -f "$APP_DIR/build/api/RAG.Orchestrator.Api.dll" ]; then
    echo -e "${YELLOW}Aplikacja nie jest jeszcze zbudowana. Uruchom:${NC}"
    echo "sudo ./deploy.sh"
fi
