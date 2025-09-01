#!/bin/bash

# RAG Suite - Nginx Configuration Generator
# Generates clean, simple nginx configuration for RAG Suite

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Configuration
APP_NAME="rag-suite"
DOMAIN_NAME="${DOMAIN_NAME:-asystent.ad.citronex.pl}"
APP_DIR="/var/www/rag-suite"
API_PORT="5000"

# SSL paths (from ssl-setup.sh)
SSL_CERT_PATH="/home/selfsigned_ad/ad.citronex.pl.pem"
SSL_KEY_PATH="/home/selfsigned_ad/ad.citronex.pl.key"

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}    RAG Suite - Nginx Setup${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}Ten skrypt musi być uruchomiony jako root (użyj sudo)${NC}"
   exit 1
fi

echo -e "${BLUE}[1/4] Sprawdzanie certyfikatów SSL...${NC}"

# Check if SSL certificates exist
if [ -f "$SSL_CERT_PATH" ] && [ -f "$SSL_KEY_PATH" ]; then
    echo -e "${GREEN}✓ Certyfikaty SSL znalezione${NC}"
    echo -e "${BLUE}[2/4] Generowanie konfiguracji HTTPS + przekierowanie HTTP...${NC}"
    
    # Create HTTPS configuration with HTTP redirect
    cat > /etc/nginx/sites-available/$APP_NAME << EOF
# RAG Suite - HTTPS Configuration
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name asystent.ad.citronex.pl www.asystent.ad.citronex.pl;
    
    # SSL Configuration
    ssl_certificate $SSL_CERT_PATH;
    ssl_certificate_key $SSL_KEY_PATH;
    
    # SSL Security
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES128-GCM-SHA256:ECDHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 10m;
    
    # Security headers (enhanced for HTTPS)
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

    # Root directory for React app
    root /var/www/rag-suite/build/web;
    index index.html;

    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_comp_level 6;
    gzip_types
        application/javascript
        application/json
        application/xml
        text/css
        text/javascript
        text/plain
        text/xml
        image/svg+xml;

    # Block sensitive files
    location ~* \.(env|log|conf|ini|sh|bak|backup|sql)$ {
        deny all;
        return 404;
    }

    # API proxy to .NET backend
    location /api/ {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
        proxy_connect_timeout 30s;
        proxy_send_timeout 30s;
        proxy_read_timeout 30s;
    }

    # Health endpoints proxy
    location /health {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        access_log off;
    }

    location /healthz/ {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
    }

    # Static assets with caching
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot|webp)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
        add_header Vary "Accept-Encoding";
    }

    # React app - handle client-side routing
    location / {
        try_files \$uri \$uri/ /index.html;
        
        # HTML files - no cache
        location ~* \.html$ {
            expires 5m;
            add_header Cache-Control "public, no-cache";
        }
    }

    # Security monitoring endpoint (local only)
    location /nginx-status {
        stub_status on;
        access_log off;
        allow 127.0.0.1;
        allow 10.0.0.0/8;
        allow 172.16.0.0/12;
        allow 192.168.0.0/16;
        deny all;
    }
}

# HTTP to HTTPS redirect
server {
    listen 80;
    listen [::]:80;
    server_name asystent.ad.citronex.pl www.asystent.ad.citronex.pl;
    return 301 https://\$server_name\$request_uri;
}
EOF

    echo -e "${GREEN}✓ Konfiguracja HTTPS utworzona${NC}"
    echo -e "${GREEN}✓ Przekierowanie HTTP → HTTPS skonfigurowane${NC}"

else
    echo -e "${YELLOW}⚠ Certyfikaty SSL nie znalezione - konfiguracja HTTP${NC}"
    echo -e "${YELLOW}Lokalizacja: $SSL_CERT_PATH${NC}"
    echo -e "${YELLOW}Lokalizacja: $SSL_KEY_PATH${NC}"
    
    echo -e "${BLUE}[2/4] Generowanie konfiguracji HTTP (port 80)...${NC}"
    
    # Create HTTP-only configuration
    cat > /etc/nginx/sites-available/$APP_NAME << 'EOF'
# RAG Suite - HTTP Configuration
server {
    listen 80;
    listen [::]:80;
    server_name asystent.ad.citronex.pl www.asystent.ad.citronex.pl;
    
    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    # Root directory for React app
    root /var/www/rag-suite/build/web;
    index index.html;

    # Gzip compression
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_comp_level 6;
    gzip_types
        application/javascript
        application/json
        application/xml
        text/css
        text/javascript
        text/plain
        text/xml
        image/svg+xml;

    # Block sensitive files
    location ~* \.(env|log|conf|ini|sh|bak|backup|sql)$ {
        deny all;
        return 404;
    }

    # API proxy to .NET backend
    location /api/ {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_connect_timeout 30s;
        proxy_send_timeout 30s;
        proxy_read_timeout 30s;
    }

    # Health endpoints proxy
    location /health {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        access_log off;
    }

    location /healthz/ {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Static assets with caching
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot|webp)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
        add_header Vary "Accept-Encoding";
    }

    # React app - handle client-side routing
    location / {
        try_files $uri $uri/ /index.html;
        
        # HTML files - no cache
        location ~* \.html$ {
            expires 5m;
            add_header Cache-Control "public, no-cache";
        }
    }

    # Security monitoring endpoint (local only)
    location /nginx-status {
        stub_status on;
        access_log off;
        allow 127.0.0.1;
        allow 10.0.0.0/8;
        allow 172.16.0.0/12;
        allow 192.168.0.0/16;
        deny all;
    }
}
EOF

    echo -e "${GREEN}✓ Konfiguracja HTTP utworzona${NC}"
fi

echo -e "${BLUE}[3/4] Aktywacja konfiguracji...${NC}"

# Remove default site if exists
if [ -f /etc/nginx/sites-enabled/default ]; then
    rm -f /etc/nginx/sites-enabled/default
    echo -e "${YELLOW}✓ Usunięto domyślną stronę nginx${NC}"
fi

# Enable new site
ln -sf /etc/nginx/sites-available/$APP_NAME /etc/nginx/sites-enabled/

# Test configuration
if nginx -t; then
    echo -e "${GREEN}✓ Konfiguracja nginx jest poprawna${NC}"
    
    # Reload nginx
    systemctl reload nginx
    echo -e "${GREEN}✓ Nginx przeładowany${NC}"
else
    echo -e "${RED}✗ Błąd w konfiguracji nginx${NC}"
    exit 1
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}    Konfiguracja nginx zakończona!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}Informacje o konfiguracji:${NC}"
echo "- Domena: $DOMAIN_NAME"
echo "- Konfiguracja: /etc/nginx/sites-available/$APP_NAME"
echo "- Web root: $APP_DIR/build/web"
echo "- API proxy: 127.0.0.1:$API_PORT"

if [ -f "$SSL_CERT_PATH" ] && [ -f "$SSL_KEY_PATH" ]; then
    echo "- HTTPS: ✓ Aktywne"
    echo "- HTTP: Przekierowanie na HTTPS"
    echo ""
    echo -e "${YELLOW}URL-e:${NC}"
    echo "- Frontend: https://$DOMAIN_NAME"
    echo "- API Health: https://$DOMAIN_NAME/health"
    echo "- System Health: https://$DOMAIN_NAME/healthz/system"
else
    echo "- HTTPS: ✗ Niedostępne (brak certyfikatów)"
    echo ""
    echo -e "${YELLOW}URL-e:${NC}"
    echo "- Frontend: http://$DOMAIN_NAME"
    echo "- API Health: http://$DOMAIN_NAME/health"
    echo "- System Health: http://$DOMAIN_NAME/healthz/system"
fi

echo ""
echo -e "${YELLOW}Przydatne komendy:${NC}"
echo "- Test nginx: sudo nginx -t"
echo "- Reload nginx: sudo systemctl reload nginx"
echo "- Status nginx: sudo systemctl status nginx"
echo "- Logi nginx: sudo tail -f /var/log/nginx/error.log"
echo "- Nginx status: curl http://localhost/nginx-status"
echo ""
