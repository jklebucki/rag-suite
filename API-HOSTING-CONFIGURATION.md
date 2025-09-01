# RAG Suite - API Hosting Configuration

## Overview

.NET API jest teraz skonfigurowane do nasłuchiwania na wszystkich interfejsach sieciowych, co umożliwia dostęp z zewnętrznych maszyn.

## Key Changes

### 1. .NET API Binding
**Zmiana w `production-setup.sh`:**
```bash
# PRZED (tylko localhost):
Environment=ASPNETCORE_URLS=http://localhost:5000

# PO (wszystkie interfejsy):
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000
```

### 2. CORS Configuration
**Zaktualizowano `ServiceCollectionExtensions.cs`:**
```csharp
policy.WithOrigins(
    "http://localhost:5173",              // Vite dev server
    "http://localhost:3000",              // React dev server  
    "http://localhost:8080",              // Alternative dev server
    "http://asystent.ad.citronex.pl",     // Production HTTP
    "https://asystent.ad.citronex.pl"     // Production HTTPS
)
```

**Włączono CORS w Production w `Program.cs`:**
```csharp
// Enable CORS for all environments (needed for direct API access)
app.UseCors("AllowFrontend");
```

### 3. Firewall Configuration
**Dodano do `production-setup.sh`:**
```bash
# Allow API port for direct access
ufw allow 5000/tcp comment "RAG API"
```

## Access Methods

### 1. Through Nginx Proxy (Recommended)
```bash
# Frontend routing through nginx
https://asystent.ad.citronex.pl/

# API routing through nginx  
https://asystent.ad.citronex.pl/api/health
curl https://asystent.ad.citronex.pl/api/health
```

### 2. Direct API Access
```bash
# Direct access to API (port 5000)
http://asystent.ad.citronex.pl:5000/health
curl http://asystent.ad.citronex.pl:5000/health

# From external machine
curl http://SERVER_IP:5000/health
```

### 3. Local Access (Server)
```bash
# Local on server
curl http://localhost:5000/health
curl http://127.0.0.1:5000/health
curl http://0.0.0.0:5000/health
```

## Network Configuration

### Firewall Rules (UFW)
```bash
# Check current rules
sudo ufw status

# Default rules set by production-setup.sh:
22/tcp     ALLOW       # SSH
80/tcp     ALLOW       # HTTP
443/tcp    ALLOW       # HTTPS
5000/tcp   ALLOW       # RAG API
```

### Port Binding
```bash
# Check what's listening on port 5000
sudo netstat -tlnp | grep :5000
sudo ss -tlnp | grep :5000

# Expected output:
tcp  0  0  0.0.0.0:5000  0.0.0.0:*  LISTEN  PID/dotnet
```

## Testing Connectivity

### From Local Machine
```bash
# Test nginx proxy
curl -I http://asystent.ad.citronex.pl/health

# Test direct API 
curl -I http://asystent.ad.citronex.pl:5000/health
```

### From External Machine
```bash
# Replace SERVER_IP with actual server IP
curl -I http://SERVER_IP:5000/health

# Test API endpoints
curl http://SERVER_IP:5000/api/health
curl http://SERVER_IP:5000/api/plugins
```

### Internal (on server)
```bash
# Local tests
curl http://localhost:5000/health
curl http://127.0.0.1:5000/health

# Check service status
sudo systemctl status rag-api
sudo journalctl -u rag-api -f
```

## Security Considerations

### 1. Direct API Access
- API port 5000 jest teraz dostępny z zewnątrz
- CORS ogranicza origins do zdefiniowanych domen
- Firewall ogranicza dostęp do potrzebnych portów

### 2. Recommended Setup
**Production:** Użyj nginx proxy (port 80/443)
```bash
https://asystent.ad.citronex.pl/api/
```

**Development/Testing:** Direct API access (port 5000)
```bash
http://asystent.ad.citronex.pl:5000/
```

### 3. Firewall Security
Jeśli chcesz ograniczyć bezpośredni dostęp do API:
```bash
# Remove direct API access
sudo ufw delete allow 5000/tcp

# API będzie dostępne tylko przez nginx proxy
```

## Troubleshooting

### API Not Accessible Externally
```bash
# Check if API is binding to all interfaces
sudo netstat -tlnp | grep :5000

# Should show: 0.0.0.0:5000 (not 127.0.0.1:5000)

# Check firewall
sudo ufw status | grep 5000

# Check API logs
sudo journalctl -u rag-api -f
```

### CORS Issues
```bash
# Check browser developer tools for CORS errors
# Verify origin is in allowed list in ServiceCollectionExtensions.cs

# Test CORS with curl
curl -H "Origin: http://asystent.ad.citronex.pl" \
     -H "Access-Control-Request-Method: GET" \
     -H "Access-Control-Request-Headers: X-Requested-With" \
     -X OPTIONS \
     http://asystent.ad.citronex.pl:5000/health
```

### Network Connectivity
```bash
# Test from external machine
telnet asystent.ad.citronex.pl 5000

# Test DNS resolution  
nslookup asystent.ad.citronex.pl

# Test ping
ping asystent.ad.citronex.pl
```

## Configuration Files Changed

1. **`production-setup.sh`**
   - Changed `ASPNETCORE_URLS` to `http://0.0.0.0:5000`
   - Added UFW firewall configuration
   - Added port 5000 to allowed ports

2. **`ServiceCollectionExtensions.cs`**
   - Added production domain to CORS origins
   - Expanded allowed origins list

3. **`Program.cs`**
   - Moved `UseCors()` outside Development-only block
   - Enabled CORS for all environments

## Migration Notes

- **Breaking Change:** API teraz nasłuchuje na wszystkich interfejsach
- **Security:** Port 5000 jest otwarty w firewall
- **CORS:** Rozszerzono allowed origins o domenę produkcyjną
- **Access:** Możliwy bezpośredni dostęp do API z zewnątrz
