# SQLite Dependencies Troubleshooting

## Problem Description
The application fails to start on Linux servers with SQLite-related errors:

### Error 1: Missing native libraries
```
System.DllNotFoundException: Unable to load shared library 'e_sqlite3' or one of its dependencies
```

### Error 2: GLIBC version mismatch
```
/lib/x86_64-linux-gnu/libc.so.6: version `GLIBC_2.28' not found
```

### Error 3: Missing SQLite assembly
```
System.IO.FileNotFoundException: Could not load file or assembly 'Microsoft.Data.Sqlite'
```

## Solutions

### Solution 1: Install SQLite Dependencies (Recommended)
Run the provided script on your Linux server:
```bash
./scripts/install-sqlite-dependencies.sh
```

### Solution 2: Manual Installation

#### Ubuntu/Debian:
```bash
sudo apt-get update
sudo apt-get install -y sqlite3 libsqlite3-dev
```

#### RHEL/CentOS/Rocky:
```bash
sudo yum install -y sqlite sqlite-devel
# or for newer versions:
sudo dnf install -y sqlite sqlite-devel
```

### Solution 3: Self-Contained Deployment
Build the application with all dependencies included:
```bash
dotnet publish -c Release -r linux-x64 --self-contained true
```

### Solution 4: Alternative Database Provider
If SQLite continues to cause issues, consider switching to PostgreSQL or SQL Server:

#### PostgreSQL:
1. Install package: `Microsoft.EntityFrameworkCore.Npgsql`
2. Update connection string in appsettings.json
3. Change `UseSqlite()` to `UseNpgsql()` in configuration

#### SQL Server:
1. Install package: `Microsoft.EntityFrameworkCore.SqlServer`
2. Update connection string in appsettings.json
3. Change `UseSqlite()` to `UseSqlServer()` in configuration

## Verification
After applying any solution:

1. Restart the service:
   ```bash
   sudo systemctl restart rag-api
   ```

2. Check service status:
   ```bash
   sudo systemctl status rag-api
   ```

3. Monitor logs:
   ```bash
   sudo journalctl -u rag-api -f
   ```

## Prevention
To prevent this issue in future deployments:
1. Use the provided installation script during server setup
2. Consider using Docker containers with pre-installed dependencies
3. Use self-contained deployments for production
