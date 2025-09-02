# Naprawa SQLite na serwerze produkcyjnym

## Analiza problemów z logów
Na podstawie logów `rag-api.log` zidentyfikowano następujące problemy:

1. **Brak natywnych bibliotek SQLite**: `e_sqlite3.so` nie może zostać załadowane
2. **Problem z GLIBC**: Wymagana wersja `GLIBC_2.28`, ale serwer ma starszą wersję
3. **Aplikacja kontynuuje działanie**: Nasza obsługa błędów działa poprawnie

## Rozwiązanie krok po kroku

### Krok 1: Uruchom skrypt instalacyjny SQLite
```bash
# Na serwerze produkcyjnym
cd /var/www/rag-suite
chmod +x scripts/install-sqlite-dependencies.sh
sudo ./scripts/install-sqlite-dependencies.sh
```

### Krok 2: Sprawdź czy problem z GLIBC został rozwiązany
```bash
# Sprawdź wersję GLIBC
ldd --version

# Sprawdź dostępne biblioteki SQLite
find /usr/lib* -name "*sqlite*" -type f 2>/dev/null
```

### Krok 3: Jeśli nadal są problemy z GLIBC - rozwiązanie alternatywne

#### Opcja A: Self-contained deployment (zalecane)
```bash
# W katalogu źródłowym na maszynie deweloperskiej
dotnet publish src/RAG.Orchestrator.Api/RAG.Orchestrator.Api.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=false \
  -o publish-selfcontained

# Przekopiuj na serwer produkcyjny
rsync -av publish-selfcontained/ user@server:/var/www/rag-suite/build/api-selfcontained/

# Zaktualizuj systemd service file
sudo nano /etc/systemd/system/rag-api.service
# Zmień ExecStart na nową ścieżkę z self-contained
```

#### Opcja B: Przełącz na InMemory database (tymczasowo)
```bash
# Ustaw zmienną środowiskową w systemd service
sudo systemctl edit rag-api

# Dodaj:
[Service]
Environment="USE_INMEMORY_DB=true"
```

#### Opcja C: Przełącz na PostgreSQL/SQL Server
```bash
# Skonfiguruj connection string w appsettings.Production.json
# Nasza aplikacja automatycznie przełączy się na odpowiedniego providera
```

### Krok 4: Restart i weryfikacja
```bash
# Restart usługi
sudo systemctl daemon-reload
sudo systemctl restart rag-api

# Sprawdź status
sudo systemctl status rag-api

# Monitoruj logi w czasie rzeczywistym
sudo journalctl -u rag-api -f
```

### Krok 5: Test funkcjonalności
```bash
# Test czy API odpowiada
curl -X GET http://localhost:5000/api/health

# Test czy admin user został utworzony (gdy database zadziała)
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@citronex.pl","password":"Citro123"}'
```

## Oczekiwane rezultaty

Po wykonaniu kroków powinieneś zobaczyć w logach:
```
info: RAG.Security.Data.SecurityDbContext[0]
      Database created successfully
info: RAG.Security.Data.SecurityDbContext[0]
      Admin user created successfully with email: admin@citronex.pl
```

## Troubleshooting

### Jeśli nadal są problemy z SQLite:
1. Sprawdź `SQLITE-TROUBLESHOOTING.md` 
2. Rozważ self-contained deployment
3. Przełącz na PostgreSQL jako database provider

### Jeśli aplikacja się nie uruchamia:
1. Sprawdź uprawnienia do plików
2. Sprawdź czy wszystkie zależności są zainstalowane
3. Sprawdź systemd logs: `sudo journalctl -u rag-api --since "1 hour ago"`

## Kontakt w razie problemów

Jeśli któryś z kroków nie działa, wyślij:
1. Output z `sudo journalctl -u rag-api --since "10 minutes ago"`
2. Output z `ldd --version`
3. Output z `find /usr/lib* -name "*sqlite*" -type f`
