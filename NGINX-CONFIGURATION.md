# RAG Suite - Nginx Configuration Guide

## Overview

Konfiguracja nginx została przepisana z myślą o prostocie i modularności. System składa się z trzech głównych skryptów:

## Architecture

### Frontend (React)
- **Port**: Serwowane przez nginx z katalogu `/var/www/rag-suite/build/web`
- **Routing**: SPA z client-side routing (`try_files $uri $uri/ /index.html`)
- **Build**: `npm run build` tworzy pliki w `dist/` kopiowane do `build/web/`

### Backend (.NET API)
- **Port**: 5000 (localhost tylko)
- **Proxy**: Nginx przekierowuje `/api/*` na `http://127.0.0.1:5000`
- **Health**: `/health` i `/healthz/*` również proxowane
- **Systemd**: Service `rag-api` zarządza procesem .NET

### Static Assets
- **Cache**: 1 rok dla JS/CSS/obrazki
- **Gzip**: Kompresja dla wszystkich tekstowych plików
- **CORS**: Nagłówki dla fontów

## Scripts

### 1. nginx-setup.sh
**Główny skrypt konfiguracji nginx**

```bash
sudo ./nginx-setup.sh
```

**Funkcje:**
- Tworzy clean konfigurację nginx
- Automatycznie wykrywa certyfikaty SSL
- Konfiguruje HTTP i HTTPS (jeśli certyfikaty dostępne)
- Ustawia przekierowanie HTTP → HTTPS
- Testuje i przeładowuje nginx

**Ścieżki certyfikatów:**
- Certificate: `/home/selfsigned_ad/ad.citronex.pl.pem`
- Private Key: `/home/selfsigned_ad/ad.citronex.pl.key`

### 2. production-setup.sh  
**Główny skrypt setupu produkcji**

```bash
sudo ./production-setup.sh
```

**Funkcje:**
- Instaluje .NET 8 (używa `install-dotnet8.sh`)
- Instaluje Node.js 20
- Instaluje nginx
- Tworzy systemd service dla API
- Wywołuje `nginx-setup.sh` dla konfiguracji nginx
- Tworzy katalogi i ustawia uprawnienia

### 3. ssl-setup.sh
**Skrypt konfiguracji SSL**

```bash
sudo ./ssl-setup.sh
```

**Funkcje:**
- Sprawdza dostępność certyfikatów wildcard
- Wywołuje `nginx-setup.sh` z SSL
- Testuje konfigurację SSL
- Przeładowuje nginx z HTTPS

## Configuration Details

### Nginx Locations

```nginx
# API proxy
location /api/ {
    proxy_pass http://127.0.0.1:5000;
    # Standard proxy headers
}

# Health endpoints
location /health {
    proxy_pass http://127.0.0.1:5000;
    access_log off;
}

location /healthz/ {
    proxy_pass http://127.0.0.1:5000;
}

# Static assets (long cache)
location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot|webp)$ {
    expires 1y;
    add_header Cache-Control "public, immutable";
}

# React SPA routing
location / {
    try_files $uri $uri/ /index.html;
}

# Security monitoring (local only)
location /nginx-status {
    stub_status on;
    allow 127.0.0.1;
    deny all;
}
```

### Security Headers

```nginx
add_header X-Frame-Options "SAMEORIGIN" always;
add_header X-Content-Type-Options "nosniff" always;
add_header X-XSS-Protection "1; mode=block" always;
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
# HTTPS only:
add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
```

### SSL Configuration

```nginx
# SSL Security
ssl_protocols TLSv1.2 TLSv1.3;
ssl_ciphers ECDHE-RSA-AES128-GCM-SHA256:ECDHE-RSA-AES256-GCM-SHA384;
ssl_prefer_server_ciphers off;
ssl_session_cache shared:SSL:10m;
ssl_session_timeout 10m;
```

## File Structure

```
/etc/nginx/sites-available/rag-suite  # Main nginx config
/var/www/rag-suite/
├── build/
│   ├── api/              # .NET compiled files
│   └── web/              # React built files
/etc/systemd/system/rag-api.service   # API service
/home/selfsigned_ad/      # SSL certificates
```

## Usage Examples

### Basic Setup
```bash
# Full production setup
sudo ./production-setup.sh

# Build and deploy applications  
sudo ./deploy.sh

# Configure SSL (if certificates available)
sudo ./ssl-setup.sh
```

### Maintenance
```bash
# Test nginx configuration
sudo nginx -t

# Reload nginx (no downtime)
sudo systemctl reload nginx

# Restart API
sudo systemctl restart rag-api

# Check status
sudo systemctl status rag-api nginx
```

### Troubleshooting
```bash
# API logs
sudo journalctl -fu rag-api

# Nginx logs
sudo tail -f /var/log/nginx/error.log

# Nginx status
curl http://localhost/nginx-status

# Test endpoints
curl http://localhost/health
curl https://asystent.ad.citronex.pl/health
```

## Key Improvements

1. **Modular**: Oddzielne skrypty dla różnych funkcji
2. **Clean**: Usunięto wszystkie problematyczne dyrektywy nginx
3. **Secure**: Proper SSL configuration z HSTS
4. **Simple**: Minimalna ale kompletna konfiguracja
5. **Testable**: Każdy skrypt testuje swoją konfigurację
6. **Maintainable**: Łatwe do debugowania i aktualizacji

## Migration Notes

- Stare pliki zachowane jako `*-old.sh`
- Konfiguracja nginx całkowicie przepisana
- Usunięto rate limiting (można dodać później w http context)
- Uproszczono proxy settings
- Dodano automatyczną detekcję SSL
