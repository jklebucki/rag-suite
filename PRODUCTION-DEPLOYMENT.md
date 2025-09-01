# RAG Suite - Production Deployment

Ten dokument opisuje proces wdrażania aplikacji RAG Suite na serwer produkcyjny Ubuntu dla domeny `asystent.ad.citronex.pl`.

## Wymagania

- Ubuntu 20.04 LTS lub nowszy
- Dostęp root (sudo)
- Dostęp do internetu dla klonowania repozytorium
- Domena `asystent.ad.citronex.pl` wskazująca na serwer
- Dostęp do Elasticsearch (elasticsearch.ad.citronex.pl)
- Dostęp do LLM (llm.ad.citronex.pl)

## Instalacja (wszystko w jednym kroku)

### Opcja 1: Super Quick Install (automatycznie wszystko)

```bash
# Instalacja z HTTP (bez SSL)
curl -sSL https://raw.githubusercontent.com/jklebucki/rag-suite/main/quick-install.sh | sudo bash -s asystent.ad.citronex.pl admin@citronex.pl n

# Instalacja z HTTPS (z SSL)
curl -sSL https://raw.githubusercontent.com/jklebucki/rag-suite/main/quick-install.sh | sudo bash -s asystent.ad.citronex.pl admin@citronex.pl y
```

### Opcja 2: Pobierz i uruchom skrypt setup

```bash
# Pobierz skrypt setup bezpośrednio z GitHub
curl -sSL https://raw.githubusercontent.com/jklebucki/rag-suite/main/production-setup.sh -o production-setup.sh
chmod +x production-setup.sh

# Uruchom skrypt (automatycznie sklonuje repo i skonfiguruje środowisko)
sudo ./production-setup.sh asystent.ad.citronex.pl
```

**Ten skrypt automatycznie:**
- ✅ Tworzy katalog `/var/www/rag-suite`
- ✅ Klonuje repozytorium z GitHub
- ✅ Instaluje .NET 8 SDK, Node.js 20, Nginx, Git
- ✅ Tworzy serwis systemd `rag-api.service`
- ✅ Konfiguruje Nginx z rate limiting i security headers
- ✅ Ustawia odpowiednie uprawnienia

### Wdrożenie aplikacji

```bash
cd /var/www/rag-suite
sudo ./deploy.sh
```

### Konfiguracja SSL (zalecane)

**Uwaga**: Skrypt SSL wykorzystuje istniejące certyfikaty wildcard z `/home/selfsigned_ad/`

```bash
cd /var/www/rag-suite
sudo ./ssl-setup.sh
```

## Alternatywna instalacja (krok po kroku)

Jeśli wolisz ręczną kontrolę:

### 1. Klonowanie repozytorium

```bash
# Sklonuj repozytorium do katalogu /var/www
sudo mkdir -p /var/www
cd /var/www
sudo git clone https://github.com/jklebucki/rag-suite.git
sudo chown -R www-data:www-data rag-suite
```

### 2. Uruchomienie skryptu konfiguracji

```bash
cd /var/www/rag-suite
sudo ./production-setup.sh asystent.ad.citronex.pl
```

### 3. Pierwsze wdrożenie

```bash
sudo ./deploy.sh
```

### 4. Konfiguracja SSL

**Uwaga**: Korzysta z istniejących certyfikatów wildcard

```bash
sudo ./ssl-setup.sh
```

## Aktualizacje

Dla kolejnych aktualizacji wystarczy uruchomić:

```bash
cd /var/www/rag-suite
sudo ./deploy.sh
```

## Struktura po deployment

```
/var/www/rag-suite/
├── build/                    # Folder build (w .gitignore)
│   ├── api/                  # Zbudowana aplikacja .NET
│   │   ├── RAG.Orchestrator.Api.dll
│   │   ├── appsettings.Production.json
│   │   └── ...
│   └── web/                  # Zbudowana aplikacja React
│       ├── index.html
│       ├── assets/
│       └── ...
├── src/                      # Kod źródłowy
├── production-setup.sh       # Skrypt konfiguracji
└── deploy.sh                # Skrypt deployment
```

## Konfiguracja

### Konfiguracja zewnętrznych serwisów

Edytuj plik `/var/www/rag-suite/build/api/appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "ElasticSearch": "http://elasticsearch.ad.citronex.pl:9200"
  },
  "LLM": {
    "Provider": "Ollama",
    "BaseUrl": "http://llm.ad.citronex.pl:11434",
    "Model": "llama3.1:8b"
  },
  "AllowedHosts": "asystent.ad.citronex.pl,localhost"
}
```

### Konfiguracja DNS

Upewnij się, że domena wskazuje na serwer:

```bash
# Sprawdź IP serwera
curl ifconfig.me

# Sprawdź czy DNS wskazuje poprawnie
nslookup asystent.ad.citronex.pl
```

### Firewall

Otwórz wymagane porty:

```bash
sudo ufw allow 22/tcp    # SSH
sudo ufw allow 80/tcp    # HTTP
sudo ufw allow 443/tcp   # HTTPS
sudo ufw enable
```

## Pliki skryptów

### `quick-install.sh` (NOWY!)
Super szybka instalacja jedną komendą - automatycznie uruchamia setup, deploy i opcjonalnie SSL

### `production-setup.sh` 
Skrypt konfiguracji pierwotnej - automatycznie klonuje repo i konfiguruje środowisko

### `deploy.sh` 
Skrypt deployment/aktualizacji - używany do aktualizacji

### `ssl-setup.sh`
Skrypt konfiguracji SSL/HTTPS z istniejącymi certyfikatami wildcard *.ad.citronex.pl

### `fix-nodejs.sh`
Skrypt naprawy problemów z Node.js na różnych wersjach Ubuntu (szczególnie 18.04)

### `health-check.sh`
Kompleksowy test wszystkich komponentów aplikacji

## Monitoring i zarządzanie

### Szybki test aplikacji

```bash
cd /var/www/rag-suite
sudo ./health-check.sh
```

### Sprawdzanie statusu

```bash
# Status serwisów
sudo systemctl status rag-api nginx

# Logi API w czasie rzeczywistym
sudo journalctl -fu rag-api

# Logi Nginx
sudo tail -f /var/log/nginx/rag-suite.error.log
sudo tail -f /var/log/nginx/rag-suite.access.log

# Nginx status (tylko lokalnie)
curl http://localhost/nginx-status
```

### Restart serwisów

```bash
# Restart API
sudo systemctl restart rag-api

# Restart Nginx
sudo systemctl restart nginx

# Przeładowanie Nginx (bez przerwy w działaniu)
sudo systemctl reload nginx
```

## Rozwiązywanie problemów

### API nie uruchamia się

```bash
# Sprawdź logi
sudo journalctl -u rag-api --no-pager -n 50

# Sprawdź czy port 5000 jest wolny
sudo netstat -tlnp | grep :5000

# Sprawdź uprawnienia
ls -la /var/www/rag-suite/build/api/
```

### Nginx zwraca 502 Bad Gateway

```bash
# Sprawdź czy API działa
curl http://localhost:5000/health

# Sprawdź konfigurację Nginx
sudo nginx -t

# Sprawdź logi Nginx
sudo tail -f /var/log/nginx/rag-suite.error.log
```

### React app nie ładuje się

```bash
# Sprawdź czy pliki istnieją
ls -la /var/www/rag-suite/build/web/

# Sprawdź uprawnienia
sudo chown -R www-data:www-data /var/www/rag-suite/build/web/
sudo chmod -R 644 /var/www/rag-suite/build/web/*
sudo find /var/www/rag-suite/build/web -type d -exec chmod 755 {} \;
```

## Struktura serwisów

### rag-api.service

Lokalizacja: `/etc/systemd/system/rag-api.service`

### Nginx konfiguracja

Lokalizacja: `/etc/nginx/sites-available/rag-suite`

## Bezpieczeństwo

- API działa na localhost:5000 (nie jest dostępne z zewnątrz)
- Nginx działa jako reverse proxy
- Wszystkie pliki należą do www-data
- Serwis ma ograniczone uprawnienia systemowe

## Backup

Zalecane jest regularne tworzenie kopii zapasowych:

```bash
# Backup konfiguracji
sudo tar -czf rag-suite-config-$(date +%Y%m%d).tar.gz \
  /etc/nginx/sites-available/rag-suite \
  /etc/systemd/system/rag-api.service \
  /var/www/rag-suite/build/api/appsettings.Production.json

# Backup aplikacji
sudo tar --exclude='/var/www/rag-suite/.git' \
  --exclude='/var/www/rag-suite/build' \
  -czf rag-suite-app-$(date +%Y%m%d).tar.gz \
  /var/www/rag-suite
```

## Rozwiązywanie problemów

### Problem z Node.js na Ubuntu 18.04

Jeśli widzisz błąd: `nodejs : Depends: libc6 (>= 2.28) but 2.27-3ubuntu1.6 is to be installed`

**Rozwiązanie:** Użyj skryptu naprawy Node.js

```bash
cd /var/www/rag-suite
sudo ./fix-nodejs.sh
```

### Inne częste problemy

#### Aplikacja nie startuje
```bash
# Sprawdź status serwisu
sudo systemctl status rag-api

# Sprawdź logi
sudo journalctl -u rag-api -f
```

#### Nginx zwraca błąd 502
```bash
# Sprawdź czy API działa
curl http://localhost:5000/health

# Sprawdź logi Nginx
sudo tail -f /var/log/nginx/error.log
```

#### Build React nie działa
```bash
# Sprawdź wersję Node.js
node --version

# Jeśli problem z zależnościami
cd /var/www/rag-suite/src/RAG.Web.UI
sudo rm -rf node_modules package-lock.json
sudo npm install
```

#### Problemy z uprawnieniami
```bash
# Napraw uprawnienia
sudo chown -R www-data:www-data /var/www/rag-suite/build
sudo chmod -R 755 /var/www/rag-suite/build
```
