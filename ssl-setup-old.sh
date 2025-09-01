#!/bin/bash

# RAG Suite SSL Setup Script
# Konfiguruje SSL/HTTPS dla domeny asystent.ad.citronex.pl z istniejącymi certyfikatami

set -e

# Kolory dla outputu
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Zmienne konfiguracyjne
DOMAIN_NAME="asystent.ad.citronex.pl"
SSL_CERT_PATH="/home/selfsigned_ad/ad.citronex.pl.pem"
SSL_KEY_PATH="/home/selfsigned_ad/ad.citronex.pl.key"

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}    RAG Suite SSL Configuration${NC}"
echo -e "${BLUE}========================================${NC}"
echo -e "${YELLOW}Domena: ${DOMAIN_NAME}${NC}"
echo -e "${YELLOW}Certyfikat: ${SSL_CERT_PATH}${NC}"
echo -e "${YELLOW}Klucz: ${SSL_KEY_PATH}${NC}"
echo ""

# Sprawdź czy skrypt jest uruchamiany jako root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}Ten skrypt musi być uruchomiony jako root (użyj sudo)${NC}"
   exit 1
fi

# Sprawdź czy nginx działa
if ! systemctl is-active --quiet nginx; then
    echo -e "${RED}Nginx nie działa. Uruchom najpierw production-setup.sh${NC}"
    exit 1
fi

echo -e "${BLUE}[1/3] Sprawdzanie certyfikatów...${NC}"

# Sprawdź czy certyfikaty istnieją
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

echo -e "${GREEN}✓ Certyfikaty znalezione${NC}"

# Sprawdź uprawnienia do certyfikatów
if [ ! -r "$SSL_CERT_PATH" ]; then
    echo -e "${YELLOW}Dostosowywanie uprawnień do certyfikatu...${NC}"
    chmod 644 "$SSL_CERT_PATH"
fi

if [ ! -r "$SSL_KEY_PATH" ]; then
    echo -e "${YELLOW}Dostosowywanie uprawnień do klucza...${NC}"
    chmod 600 "$SSL_KEY_PATH"
    chown root:root "$SSL_KEY_PATH"
fi

# Sprawdź ważność certyfikatu
echo -e "${YELLOW}Sprawdzanie ważności certyfikatu...${NC}"
CERT_INFO=$(openssl x509 -in "$SSL_CERT_PATH" -text -noout)
CERT_SUBJECT=$(echo "$CERT_INFO" | grep "Subject:" || echo "Nie znaleziono Subject")
CERT_EXPIRES=$(openssl x509 -in "$SSL_CERT_PATH" -enddate -noout | cut -d= -f2)

echo -e "${YELLOW}Subject: ${CERT_SUBJECT}${NC}"
echo -e "${YELLOW}Wygasa: ${CERT_EXPIRES}${NC}"

# Sprawdź czy certyfikat zawiera naszą domenę
if openssl x509 -in "$SSL_CERT_PATH" -text -noout | grep -q "ad.citronex.pl"; then
    echo -e "${GREEN}✓ Certyfikat zawiera domenę ad.citronex.pl${NC}"
else
    echo -e "${YELLOW}⚠ Sprawdź czy certyfikat obejmuje domenę ad.citronex.pl${NC}"
fi

echo -e "${BLUE}[2/3] Konfiguracja Nginx SSL...${NC}"

# Backup obecnej konfiguracji
cp /etc/nginx/sites-available/rag-suite /etc/nginx/sites-available/rag-suite.backup-$(date +%Y%m%d-%H%M%S)

# Dodaj konfigurację SSL do istniejącej konfiguracji Nginx
cat > /etc/nginx/sites-available/rag-suite << EOF
# RAG Suite Configuration for $DOMAIN_NAME with SSL
server {
    listen 80;
    listen [::]:80;
    server_name $DOMAIN_NAME www.$DOMAIN_NAME;
    
    # Redirect all HTTP to HTTPS
    return 301 https://\$server_name\$request_uri;
}

server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name $DOMAIN_NAME www.$DOMAIN_NAME;
    root /var/www/rag-suite/build/web;
    index index.html;

    # SSL Configuration
    ssl_certificate $SSL_CERT_PATH;
    ssl_certificate_key $SSL_KEY_PATH;
    
    # SSL Security Settings
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-RSA-AES128-GCM-SHA256:ECDHE-RSA-AES256-GCM-SHA384:ECDHE-RSA-AES128-SHA256:ECDHE-RSA-AES256-SHA384;
    ssl_prefer_server_ciphers off;
    ssl_session_cache shared:SSL:10m;
    ssl_session_timeout 10m;
    
    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    add_header X-Robots-Tag "noindex, nofollow" always;
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
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
        
        proxy_pass http://localhost:5000/api/;
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
        proxy_pass http://localhost:5000/health;
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
        proxy_pass http://localhost:5000/healthz/;
        proxy_http_version 1.1;
        proxy_set_header Host \$host;
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

    # Monitoring endpoint dla ops (tylko lokalne IP)
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
    access_log /var/log/nginx/rag-suite.access.log combined;
    error_log /var/log/nginx/rag-suite.error.log warn;
}
EOF

echo -e "${GREEN}✓ Konfiguracja Nginx SSL została utworzona${NC}"

echo -e "${BLUE}[3/3] Restart i weryfikacja SSL...${NC}"

# Test konfiguracji Nginx
if nginx -t; then
    echo -e "${GREEN}✓ Konfiguracja Nginx jest poprawna${NC}"
    
    # Restart Nginx
    systemctl restart nginx
    if systemctl is-active --quiet nginx; then
        echo -e "${GREEN}✓ Nginx został zrestartowany${NC}"
    else
        echo -e "${RED}✗ Błąd podczas restartowania Nginx${NC}"
        systemctl status nginx
        exit 1
    fi
else
    echo -e "${RED}✗ Błąd w konfiguracji Nginx${NC}"
    nginx -t
    exit 1
fi

# Sprawdź czy porty 80 i 443 są otwarte
if netstat -tulpn | grep -q ":80 "; then
    echo -e "${GREEN}✓ Port 80 (HTTP) jest aktywny${NC}"
else
    echo -e "${YELLOW}⚠ Port 80 nie jest aktywny${NC}"
fi

if netstat -tulpn | grep -q ":443 "; then
    echo -e "${GREEN}✓ Port 443 (HTTPS) jest aktywny${NC}"
else
    echo -e "${YELLOW}⚠ Port 443 nie jest aktywny${NC}"
fi

# Informacja o backupie
backup_file=$(ls -1t /etc/nginx/sites-available/rag-suite.backup-* 2>/dev/null | head -1)
if [ -n "$backup_file" ]; then
    echo -e "${BLUE}ℹ Backup poprzedniej konfiguracji: $backup_file${NC}"
fi

echo -e "\n${GREEN}═══════════════════════════════════════${NC}"
echo -e "${GREEN}      SSL SKONFIGUROWANY POMYŚLNIE      ${NC}"
echo -e "${GREEN}═══════════════════════════════════════${NC}"
echo
echo -e "${BLUE}🔒 Aplikacja dostępna pod adresem:${NC}"
echo -e "   ${YELLOW}https://$DOMAIN_NAME${NC}"
echo
echo -e "${BLUE}📋 Informacje o certyfikacie:${NC}"
echo -e "   Certyfikat: $SSL_CERT_PATH"
echo -e "   Klucz prywatny: $SSL_KEY_PATH"
echo
echo -e "${BLUE}⚙️ Przydatne komendy:${NC}"
echo -e "   ${CYAN}nginx -t${NC}                    - Test konfiguracji"
echo -e "   ${CYAN}systemctl reload nginx${NC}      - Reload bez restaru"
echo -e "   ${CYAN}systemctl status nginx${NC}      - Status serwisu"
echo -e "   ${CYAN}openssl x509 -in $SSL_CERT_PATH -text -noout${NC} - Sprawdź certyfikat"
echo
echo -e "${BLUE}📄 Logi dostępne w:${NC}"
echo -e "   /var/log/nginx/rag-suite.access.log"
echo -e "   /var/log/nginx/rag-suite.error.log"
echo
echo -e "${GREEN}Sprawdź działanie: curl -I https://$DOMAIN_NAME${NC}"
echo

# Test konfiguracji nginx
nginx -t
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Konfiguracja Nginx jest poprawna${NC}"
    systemctl reload nginx
else
    echo -e "${RED}✗ Błąd w konfiguracji Nginx${NC}"
    # Przywróć backup
    cp /etc/nginx/sites-available/rag-suite.pre-ssl /etc/nginx/sites-available/rag-suite
    systemctl reload nginx
fi

echo ""
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}    SSL skonfigurowany pomyślnie!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${YELLOW}Informacje o certyfikacie:${NC}"
certbot certificates

echo ""
echo -e "${YELLOW}Aplikacja jest teraz dostępna przez HTTPS:${NC}"
echo "- Frontend: https://$DOMAIN_NAME"
echo "- API Health: https://$DOMAIN_NAME/health"
echo ""
echo -e "${YELLOW}Testowanie SSL:${NC}"
echo "- SSL Labs: https://www.ssllabs.com/ssltest/analyze.html?d=$DOMAIN_NAME"
echo "- Qualys: https://www.qualys.com/forms/freescan/"
echo ""
echo -e "${YELLOW}Zarządzanie certyfikatem:${NC}"
echo "- Status: sudo certbot certificates"
echo "- Odnowienie: sudo certbot renew"
echo "- Test odnowienia: sudo certbot renew --dry-run"
echo "- Logi: sudo tail -f /var/log/letsencrypt/letsencrypt.log"
echo ""

# Test końcowy HTTPS
echo -e "${YELLOW}Test połączenia HTTPS...${NC}"
if curl -s -I https://$DOMAIN_NAME/health > /dev/null; then
    echo -e "${GREEN}✓ HTTPS działa poprawnie${NC}"
else
    echo -e "${YELLOW}⚠ Sprawdź konfigurację HTTPS${NC}"
fi
