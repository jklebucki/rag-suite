# Przełączenie na PostgreSQL - Instrukcje

## ✅ Wykonane zmiany

1. **Usunięto wszystkie zależności SQLite** z projektów:
   - Microsoft.EntityFrameworkCore.Sqlite
   - Microsoft.Data.Sqlite
   - SQLitePCLRaw.bundle_e_sqlite3
   - Microsoft.EntityFrameworkCore.InMemory

2. **Dodano PostgreSQL** do projektów:
   - Npgsql.EntityFrameworkCore.PostgreSQL (wersja 8.0.4)
   - Microsoft.EntityFrameworkCore.Design (dla migracji)

3. **Zaktualizowano DbContext** - wszystkie tabele i kolumny będą tworzone małymi literami z podkreślnikami:
   - `Users` → `users`
   - `UserRoles` → `user_roles`
   - `UserClaims` → `user_claims`
   - `FirstName` → `first_name`
   - itd.

4. **Zaktualizowano connection stringi** we wszystkich plikach appsettings:
   ```json
   "ConnectionStrings": {
     "SecurityDatabase": "Host=localhost;Database=rag-suite;Username=postgres;Password=postgres"
   }
   ```

## 🚀 Kroki do uruchomienia na serwerze

### 1. Instalacja PostgreSQL na serwerze Linux

```bash
# Ubuntu/Debian
sudo apt update
sudo apt install postgresql postgresql-contrib

# CentOS/RHEL
sudo yum install postgresql-server postgresql-contrib
sudo postgresql-setup initdb

# Fedora
sudo dnf install postgresql-server postgresql-contrib
sudo postgresql-setup --initdb

# Start serwisu
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

### 2. Konfiguracja PostgreSQL

```bash
# Przełącz się na użytkownika postgres
sudo -u postgres psql

# W konsoli PostgreSQL:
CREATE DATABASE "rag-suite";
CREATE USER postgres WITH PASSWORD 'postgres';
GRANT ALL PRIVILEGES ON DATABASE "rag-suite" TO postgres;
\q
```

### 3. Konfiguracja PostgreSQL dla zewnętrznych połączeń

```bash
# Edytuj postgresql.conf
sudo nano /etc/postgresql/*/main/postgresql.conf
# Zmień: listen_addresses = 'localhost'

# Edytuj pg_hba.conf
sudo nano /etc/postgresql/*/main/pg_hba.conf
# Dodaj: host    all             all             127.0.0.1/32            md5

# Restart PostgreSQL
sudo systemctl restart postgresql
```

### 4. Deploy aplikacji

```bash
# Build z PostgreSQL
dotnet publish src/RAG.Orchestrator.Api/RAG.Orchestrator.Api.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o publish

# Copy do serwera
rsync -av publish/ user@server:/var/www/rag-suite/build/api/

# Update systemd service jeśli potrzeba
sudo systemctl restart rag-api
```

### 5. Weryfikacja

```bash
# Sprawdź czy PostgreSQL działa
sudo systemctl status postgresql

# Test połączenia
psql -h localhost -U postgres -d rag-suite -c "SELECT version();"

# Sprawdź logi aplikacji
sudo journalctl -u rag-api -f
```

## 📋 Oczekiwane rezultaty

Po uruchomieniu aplikacji powinieneś zobaczyć w logach:

```
info: RAG.Security.Data.SecurityDbContext[0]
      Attempting to ensure PostgreSQL database is created...
info: RAG.Security.Data.SecurityDbContext[0]
      PostgreSQL database creation successful
info: RAG.Security.Data.SecurityDbContext[0]
      Admin user created successfully with email: admin@citronex.pl
```

## 🔧 Struktura tabel PostgreSQL

Tabele zostaną utworzone z małymi literami:
- `users` (zamiast Users)
- `roles` (zamiast Roles)  
- `user_roles` (zamiast UserRoles)
- `user_claims` (zamiast UserClaims)
- `role_claims` (zamiast RoleClaims)
- `user_logins` (zamiast UserLogins)
- `user_tokens` (zamiast UserTokens)

Kolumny również z podkreślnikami:
- `first_name` (zamiast FirstName)
- `last_name` (zamiast LastName)
- `created_at` (zamiast CreatedAt)

## 🎯 Podsumowanie

✅ **Wszystkie zależności SQLite usunięte**
✅ **PostgreSQL skonfigurowany z konwencją snake_case**
✅ **Connection stringi zaktualizowane**
✅ **Admin user: admin@citronex.pl / Citro123**
✅ **Database: rag-suite na localhost**

Aplikacja automatycznie utworzy bazę danych i admin usera przy pierwszym uruchomieniu!
