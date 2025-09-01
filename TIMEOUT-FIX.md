# RAG Suite - Timeout Configuration Fix

## Problem
Podczas długich operacji chat (LLM responses) występuje błąd **504 Gateway Timeout** po ~30 sekundach, mimo że:
- Frontend axios ma timeout 15 minut (900000ms)
- Backend .NET ma timeout 15 minut (`Chat:RequestTimeoutMinutes`)

## Przyczyna
Nginx proxy miał zbyt krótkie timeouty:
```nginx
proxy_send_timeout 30s;
proxy_read_timeout 30s;
```

## Rozwiązanie

### 1. **Automatyczne naprawienie (Zalecane)**
```bash
sudo ./fix-nginx-timeouts.sh
```

### 2. **Manualne naprawienie**
Edytuj `/etc/nginx/sites-available/rag-suite`:
```nginx
location /api/ {
    # ... inne ustawienia ...
    proxy_connect_timeout 30s;    # Pozostaw 30s (czas połączenia)
    proxy_send_timeout 900s;      # Zmień na 15 minut
    proxy_read_timeout 900s;      # Zmień na 15 minut
}
```

Następnie:
```bash
sudo nginx -t && sudo systemctl reload nginx
```

### 3. **Pełna regeneracja konfiguracji**
```bash
sudo ./nginx-setup.sh
```

## Weryfikacja
Po zmianie timeoutów chat operations powinny działać przez pełne 15 minut bez błędu 504.

## Konfiguracja timeoutów w systemie

| Komponent | Timeout | Plik | Ustawienie |
|-----------|---------|------|------------|
| Frontend Axios | 15 min | `api.ts` | `timeout: 900000` |
| .NET Backend | 15 min | `appsettings.json` | `Chat:RequestTimeoutMinutes: 15` |
| Nginx Proxy | 15 min | `/etc/nginx/sites-available/rag-suite` | `proxy_*_timeout 900s` |
| Nginx Connect | 30s | `/etc/nginx/sites-available/rag-suite` | `proxy_connect_timeout 30s` |

## Logi błędów
Przed poprawką:
```
504 Gateway Timeout
Failed to load resource: the server responded with a status of 504
```

Po poprawce:
- Brak błędów 504
- Chat response po pełnym czasie LLM generation
