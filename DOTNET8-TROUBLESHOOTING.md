# Rozwiązywanie problemów z .NET 8

## Problem: NETSDK1045 - Current .NET SDK does not support targeting .NET 8.0

### Objaw błędu:
```
error NETSDK1045: The current .NET SDK does not support targeting .NET 8.0. 
Either target .NET 6.0 or lower, or use a version of the .NET SDK that supports .NET 8.0.
```

### Przyczyna:
- Masz zainstalowany starszy .NET SDK (np. 6.0.414)
- Aplikacja RAG Suite wymaga .NET 8.0
- Brak .NET 8 SDK w systemie

### ✅ Szybkie rozwiązanie:

```bash
cd /var/www/rag-suite
sudo ./install-dotnet8.sh
```

### Co robi skrypt `install-dotnet8.sh`:

1. **Autodetekcja Ubuntu** - wykrywa wersję Ubuntu (18.04, 20.04, 22.04, 24.04)
2. **Sprawdza istniejące .NET** - nie usuwa obecnych wersji
3. **Instaluje .NET 8 SDK** - dodaje brakującą wersję
4. **Testuje kompilację** - sprawdza czy aplikacja się buduje
5. **Bezpieczna instalacja** - zachowuje wszystkie istniejące wersje

### Weryfikacja instalacji:

```bash
# Sprawdź wszystkie zainstalowane SDK
dotnet --list-sdks

# Powinno pokazać coś takiego:
# 6.0.414 [/usr/share/dotnet/sdk]
# 8.0.xxx [/usr/share/dotnet/sdk]

# Test kompilacji
cd /var/www/rag-suite
dotnet build src/RAG.Orchestrator.Api/RAG.Orchestrator.Api.csproj
```

### Alternatywne metody (jeśli skrypt nie działa):

#### Metoda 1: Ręczna instalacja

```bash
# Dla Ubuntu 18.04
wget https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# Dla Ubuntu 20.04
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# Dla Ubuntu 22.04+
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-8.0
```

#### Metoda 2: .NET Install Script

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version latest --channel 8.0
export PATH=$PATH:~/.dotnet
```

### Diagnostyka problemów:

```bash
# Uruchom diagnostykę - automatycznie wykrywa problem z .NET 8
cd /var/www/rag-suite
./diagnose.sh
```

### Po instalacji .NET 8:

```bash
# Pełny deployment
cd /var/www/rag-suite
sudo ./deploy.sh

# Sprawdź status
./diagnose.sh
```

### Uwagi:

- ✅ Skrypt **nie usuwa** istniejących wersji .NET
- ✅ Wspiera wszystkie wersje Ubuntu (18.04 - 24.04)
- ✅ Automatycznie testuje kompilację aplikacji
- ✅ Zawiera fallback dla nieznanych wersji Ubuntu
- ⚠️ Wymaga uprawnień root (sudo)

### Troubleshooting:

Jeśli nadal masz problemy:

1. **Sprawdź wersję Ubuntu**: `lsb_release -a`
2. **Sprawdź dostępne .NET**: `dotnet --list-sdks`
3. **Uruchom pełną diagnostykę**: `./diagnose.sh`
4. **Spróbuj pełnego setup**: `sudo ./production-setup.sh`

---

📖 **Pełna dokumentacja**: `PRODUCTION-DEPLOYMENT.md`  
🚀 **Szybki start**: `ADMIN-QUICKSTART.md`
