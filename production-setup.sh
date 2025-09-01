#!/bin/bash

# RAG Suite Production Setup Script
# Konfiguruje środowisko produkcyjne na Ubuntu Server
# Tworzy serwisy systemd, konfiguruje nginx dla aplikacji .NET i React

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
API_PORT="5000"
DOMAIN_NAME="${1:-asystent.ad.citronex.pl}"
GIT_REPO="${2:-https://github.com/jklebucki/rag-suite.git}"

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}    RAG Suite Production Setup${NC}"
echo -e "${BLUE}========================================${NC}"
echo -e "${YELLOW}Domena: ${DOMAIN_NAME}${NC}"
echo -e "${YELLOW}Port API: ${API_PORT}${NC}"
echo -e "${YELLOW}Katalog aplikacji: ${APP_DIR}${NC}"
echo -e "${YELLOW}Repozytorium Git: ${GIT_REPO}${NC}"
echo ""

# Sprawdź czy skrypt jest uruchamiany jako root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}Ten skrypt musi być uruchomiony jako root (użyj sudo)${NC}"
   exit 1
fi

echo -e "${BLUE}[1/7] Przygotowanie katalogu aplikacji...${NC}"

# Sprawdź czy katalog aplikacji istnieje
if [ ! -d "$APP_DIR" ]; then
   echo -e "${YELLOW}Katalog $APP_DIR nie istnieje. Tworzenie i klonowanie repozytorium...${NC}"
   
   # Utwórz katalog /var/www jeśli nie istnieje
   mkdir -p /var/www
   
   # Sprawdź czy git jest zainstalowany
   if ! command -v git &> /dev/null; then
       echo -e "${YELLOW}Instalacja Git...${NC}"
       apt update
       apt install -y git
   fi
   
   # Klonuj repozytorium
   echo -e "${YELLOW}Klonowanie repozytorium z $GIT_REPO...${NC}"
   cd /var/www
   git clone $GIT_REPO
   
   if [ ! -d "$APP_DIR" ]; then
       echo -e "${RED}Błąd podczas klonowania repozytorium!${NC}"
       exit 1
   fi
   
   echo -e "${GREEN}✓ Repozytorium zostało sklonowane do $APP_DIR${NC}"
else
   echo -e "${GREEN}✓ Katalog aplikacji $APP_DIR już istnieje${NC}"
   
   # Sprawdź czy to repozytorium git
   if [ ! -d "$APP_DIR/.git" ]; then
       echo -e "${YELLOW}⚠ Katalog istnieje ale nie jest repozytorium Git${NC}"
       echo -e "${YELLOW}Sprawdź zawartość katalogu $APP_DIR${NC}"
   fi
fi

echo -e "${BLUE}[2/7] Aktualizacja systemu i instalacja zależności...${NC}"
apt update && apt upgrade -y
apt install -y nginx systemd curl wget

# Sprawdź czy .NET 8 jest zainstalowany
if ! command -v dotnet &> /dev/null; then
    echo -e "${YELLOW}Instalacja .NET 8 SDK...${NC}"
    wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    apt update
    apt install -y dotnet-sdk-8.0
fi

# Sprawdź czy Node.js jest zainstalowany
if ! command -v node &> /dev/null; then
    echo -e "${YELLOW}Instalacja Node.js LTS (kompatybilna z Ubuntu 18.04)...${NC}"
    
    # Sprawdź wersję Ubuntu
    UBUNTU_VERSION=$(lsb_release -rs)
    if [[ "$UBUNTU_VERSION" == "18.04" ]]; then
        echo -e "${BLUE}Wykryto Ubuntu 18.04 - instalacja Node.js 16 LTS${NC}"
        curl -fsSL https://deb.nodesource.com/setup_16.x | bash -
    elif [[ "$UBUNTU_VERSION" == "20.04" ]]; then
        echo -e "${BLUE}Wykryto Ubuntu 20.04 - instalacja Node.js 18 LTS${NC}"
        curl -fsSL https://deb.nodesource.com/setup_18.x | bash -
    elif [[ "$UBUNTU_VERSION" == "22.04" ]] || [[ "$UBUNTU_VERSION" == "24.04" ]]; then
        echo -e "${BLUE}Wykryto Ubuntu $UBUNTU_VERSION - instalacja Node.js 20${NC}"
        curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
    else
        echo -e "${YELLOW}Nieznana wersja Ubuntu ($UBUNTU_VERSION) - próba instalacji Node.js 16 LTS${NC}"
        curl -fsSL https://deb.nodesource.com/setup_16.x | bash -
    fi
    
    apt install -y nodejs
    
    # Sprawdź czy instalacja się powiodła
    if command -v node &> /dev/null; then
        echo -e "${GREEN}✓ Node.js $(node --version) zainstalowany pomyślnie${NC}"
        echo -e "${GREEN}✓ npm $(npm --version) dostępny${NC}"
    else
        echo -e "${RED}✗ Błąd instalacji Node.js${NC}"
        exit 1
    fi
fi

echo -e "${BLUE}[3/7] Tworzenie serwisu systemd dla RAG API...${NC}"
cat > /etc/systemd/system/rag-api.service << EOF
[Unit]
Description=RAG Suite API
After=network.target

[Service]
Type=notify
User=$APP_USER
Group=$APP_USER
WorkingDirectory=$APP_DIR/build/api
ExecStart=/usr/bin/dotnet RAG.Orchestrator.Api.dll
Restart=always
RestartSec=10
TimeoutStopSec=30
SyslogIdentifier=rag-api
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:$API_PORT
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

# Ustawienia bezpieczeństwa
NoNewPrivileges=yes
PrivateTmp=yes
ProtectSystem=strict
ReadWritePaths=$APP_DIR
ProtectHome=yes

[Install]
WantedBy=multi-user.target
EOF

echo -e "${BLUE}[4/7] Konfiguracja Nginx...${NC}"

# Backup istniejącej konfiguracji nginx
if [ -f /etc/nginx/sites-available/default ]; then
    cp /etc/nginx/sites-available/default /etc/nginx/sites-available/default.backup
fi

# Tworzenie konfiguracji nginx dla RAG Suite
cat > /etc/nginx/sites-available/$APP_NAME << EOF
# RAG Suite Configuration for $DOMAIN_NAME
server {
    listen 80;
    listen [::]:80;
    server_name $DOMAIN_NAME www.$DOMAIN_NAME;
    root $APP_DIR/build/web;
    index index.html;

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header X-Robots-Tag "noindex, nofollow" always;
    add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self' https:; frame-ancestors 'none';" always;

    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_comp_level 6;
    gzip_proxied expired no-cache no-store private must-revalidate auth;
    gzip_types
        application/atom+xml
        application/geo+json
        application/javascript
        application/x-javascript
        application/json
        application/ld+json
        application/manifest+json
        application/rdf+xml
        application/rss+xml
        application/xhtml+xml
        application/xml
        font/eot
        font/otf
        font/ttf
        image/svg+xml
        text/css
        text/javascript
        text/plain
        text/xml;

    # Rate limiting for API
    limit_req_zone \$binary_remote_addr zone=api:10m rate=10r/s;
    limit_req_zone \$binary_remote_addr zone=general:10m rate=30r/s;

    # API proxy with rate limiting
    location /api/ {
        limit_req zone=api burst=20 nodelay;
        
        proxy_pass http://localhost:$API_PORT/api/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_set_header X-Forwarded-Host \$server_name;
        proxy_cache_bypass \$http_upgrade;
        
        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 300s;
        proxy_read_timeout 300s;
        
        # Buffer settings
        proxy_buffering on;
        proxy_buffer_size 4k;
        proxy_buffers 8 4k;
        proxy_busy_buffers_size 8k;
        
        # Headers for better debugging
        add_header X-Proxy-Cache \$upstream_cache_status always;
    }

    # Health check endpoint (no rate limiting)
    location /health {
        proxy_pass http://localhost:$API_PORT/health;
        proxy_http_version 1.1;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_connect_timeout 5s;
        proxy_read_timeout 5s;
        access_log off;
    }

    # System health endpoint (no rate limiting)
    location /healthz/ {
        proxy_pass http://localhost:$API_PORT/healthz/;
        proxy_http_version 1.1;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_connect_timeout 5s;
        proxy_read_timeout 5s;
        access_log off;
    }

    # Block common attack patterns
    location ~* /\.(?!well-known\/) {
        deny all;
        return 404;
    }

    # Block access to sensitive files
    location ~* \.(sql|log|conf|ini|sh|bak|backup|old|tmp)$ {
        deny all;
        return 404;
    }

    # Static assets with long cache
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot|webp|avif)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
        add_header Vary "Accept-Encoding";
        
        # CORS for fonts
        location ~* \.(woff|woff2|ttf|eot)$ {
            add_header Access-Control-Allow-Origin "*";
            expires 1y;
            add_header Cache-Control "public, immutable";
        }
    }

    # React app - wszystkie pozostałe ścieżki
    location / {
        limit_req zone=general burst=50 nodelay;
        
        try_files \$uri \$uri/ /index.html;
        
        # Cache dla HTML
        location ~* \.html$ {
            expires 5m;
            add_header Cache-Control "public, must-revalidate";
        }
        
        # Security dla index.html
        location = /index.html {
            add_header X-Frame-Options "SAMEORIGIN" always;
            add_header X-Content-Type-Options "nosniff" always;
            expires 5m;
        }
    }

    # Monitoring endpoint dla ops
    location /nginx-status {
        stub_status on;
        access_log off;
        allow 127.0.0.1;
        allow 10.0.0.0/8;
        allow 172.16.0.0/12;
        allow 192.168.0.0/16;
        deny all;
    }

    # Logs z rotacją
    access_log /var/log/nginx/$APP_NAME.access.log combined;
    error_log /var/log/nginx/$APP_NAME.error.log warn;
}

# Redirect www to non-www (optional)
server {
    listen 80;
    listen [::]:80;
    server_name www.$DOMAIN_NAME;
    return 301 http://$DOMAIN_NAME\$request_uri;
}
EOF

# Wyłącz domyślną stronę nginx
if [ -L /etc/nginx/sites-enabled/default ]; then
    unlink /etc/nginx/sites-enabled/default
fi

# Włącz konfigurację RAG Suite
ln -sf /etc/nginx/sites-available/$APP_NAME /etc/nginx/sites-enabled/

echo -e "${BLUE}[5/7] Tworzenie katalogów i ustawianie uprawnień...${NC}"

# Tworzenie katalogów build
mkdir -p $APP_DIR/build/api
mkdir -p $APP_DIR/build/web
mkdir -p /var/log/rag-suite

# Ustawienie właściciela
chown -R $APP_USER:$APP_USER $APP_DIR
chown -R $APP_USER:$APP_USER /var/log/rag-suite

# Ustawienie uprawnień
chmod -R 755 $APP_DIR
chmod -R 644 $APP_DIR/build/web/* 2>/dev/null || true

echo -e "${BLUE}[6/7] Weryfikacja konfiguracji...${NC}"

# Test konfiguracji nginx
if nginx -t; then
    echo -e "${GREEN}✓ Konfiguracja Nginx jest poprawna${NC}"
else
    echo -e "${RED}✗ Błąd w konfiguracji Nginx${NC}"
    exit 1
fi

# Sprawdź wersje zainstalowanych narzędzi
echo -e "${YELLOW}Zainstalowane wersje:${NC}"
echo "- .NET: $(dotnet --version)"
echo "- Node.js: $(node --version)"
echo "- NPM: $(npm --version)"
echo "- Nginx: $(nginx -v 2>&1 | cut -d: -f2 | cut -d/ -f2)"

echo -e "${BLUE}[7/7] Restart serwisów...${NC}"

# Przeładuj systemd
systemctl daemon-reload

# Włącz serwisy (ale nie uruchamiaj jeszcze)
systemctl enable rag-api
systemctl enable nginx

# Restart nginx
systemctl restart nginx

if systemctl is-active --quiet nginx; then
    echo -e "${GREEN}✓ Nginx został uruchomiony${NC}"
else
    echo -e "${RED}✗ Problem z uruchomieniem Nginx${NC}"
    systemctl status nginx --no-pager
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}    Setup zakończony pomyślnie!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}Następne kroki:${NC}"
echo "1. Uruchom skrypt deploy.sh aby zbudować i wdrożyć aplikację:"
echo "   cd $APP_DIR && sudo ./deploy.sh"
echo "2. Opcjonalnie skonfiguruj SSL:"
echo "   cd $APP_DIR && sudo ./ssl-setup.sh admin@citronex.pl"
echo "3. Aplikacja będzie dostępna pod adresem: http://$DOMAIN_NAME"
echo ""
echo -e "${YELLOW}Konfiguracja SSL (zalecane dla produkcji):${NC}"
echo "sudo apt install certbot python3-certbot-nginx"
echo "sudo certbot --nginx -d $DOMAIN_NAME"
echo ""
echo -e "${YELLOW}Użyteczne komendy:${NC}"
echo "- Status API: sudo systemctl status rag-api"
echo "- Logi API: sudo journalctl -fu rag-api"
echo "- Status Nginx: sudo systemctl status nginx"
echo "- Logi Nginx: sudo tail -f /var/log/nginx/$APP_NAME.error.log"
echo "- Test konfiguracji: sudo nginx -t"
echo "- Reload Nginx: sudo systemctl reload nginx"
echo ""

# Sprawdź czy aplikacja jest już zbudowana
if [ ! -f "$APP_DIR/build/api/RAG.Orchestrator.Api.dll" ]; then
    echo -e "${YELLOW}Aplikacja nie jest jeszcze zbudowana. Uruchom:${NC}"
    echo "sudo ./deploy.sh"
fi
