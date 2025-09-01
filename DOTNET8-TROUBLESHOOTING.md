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

#### Metoda 1: Ręczna instalacja Microsoft Repository (Ubuntu 18.04/20.04)

```bash
# WAŻNE: Ubuntu 18.04 i 20.04 MUSZĄ używać Microsoft repository
# Ubuntu nie dostarcza .NET 8 dla tych wersji

# Dla Ubuntu 18.04
sudo apt-get install -y wget apt-transport-https software-properties-common gpg

# Dodaj Microsoft signing key
wget -qO- https://packages.microsoft.com/keys/microsoft.asc | sudo gpg --dearmor > microsoft.asc.gpg
sudo mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/
sudo chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg

# Dodaj Microsoft repository
wget -q https://packages.microsoft.com/config/ubuntu/18.04/prod.list
sudo mv prod.list /etc/apt/sources.list.d/microsoft-prod.list
sudo chown root:root /etc/apt/sources.list.d/microsoft-prod.list

# Instaluj .NET 8
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# Dla Ubuntu 20.04 - zmień 18.04 na 20.04 w URL
```

#### Metoda 2: .NET Install Script (uniwersalna)

```bash
# Jeśli Microsoft repository nie działa
curl -sSL https://dot.net/v1/dotnet-install.sh | sudo bash -s -- --version latest --channel 8.0 --install-dir /usr/share/dotnet

# Dodaj do PATH
echo 'export PATH=$PATH:/usr/share/dotnet' | sudo tee -a /etc/environment
echo 'export DOTNET_ROOT=/usr/share/dotnet' | sudo tee -a /etc/environment

# Utwórz symlink
sudo ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet

# Restartuj sesję lub załaduj zmienne
source /etc/environment
```

#### Metoda 3: Manual Binary Installation

```bash
# Pobierz najnowszą wersję .NET 8
cd /tmp
wget https://download.visualstudio.microsoft.com/download/pr/$(curl -s https://api.nuget.org/v3-flatcontainer/microsoft.netcore.app/index.json | grep -o '"8\.[0-9]\+\.[0-9]\+"' | sort -V | tail -1 | tr -d '"')/linux-x64/dotnet-sdk-$(curl -s https://api.nuget.org/v3-flatcontainer/microsoft.netcore.app/index.json | grep -o '"8\.[0-9]\+\.[0-9]\+"' | sort -V | tail -1 | tr -d '"')-linux-x64.tar.gz

# Lub użyj bezpośredniego linka do najnowszej stabilnej wersji
wget https://dotnetcli.azureedge.net/dotnet/Sdk/LTS/dotnet-sdk-linux-x64.tar.gz

# Rozpakuj
sudo mkdir -p /usr/share/dotnet
sudo tar zxf dotnet-sdk-*-linux-x64.tar.gz -C /usr/share/dotnet

# Ustaw uprawnienia i linki
sudo chown -R root:root /usr/share/dotnet
sudo ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet

# Dodaj zmienne środowiskowe
echo 'export PATH=$PATH:/usr/share/dotnet' | sudo tee -a /etc/environment
echo 'export DOTNET_ROOT=/usr/share/dotnet' | sudo tee -a /etc/environment
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

### ⚠️ Ważne dla Ubuntu 18.04/20.04:

**Ubuntu 18.04 i 20.04 NIE MAJĄ wbudowanego wsparcia dla .NET 8!**

- ❌ **Ubuntu feeds** - .NET 8 niedostępny
- ❌ **Ubuntu backports** - nie obsługuje starszych Ubuntu
- ✅ **Microsoft repository** - jedyna oficjalna opcja
- ✅ **.NET install script** - alternatywa manual

**Dlaczego tradycyjna metoda nie działa:**
```bash
# To NIE ZADZIAŁA na Ubuntu 18.04:
sudo apt install dotnet-sdk-8.0  # Package nie istnieje w Ubuntu feeds

# To ZADZIAŁA:
# 1. Najpierw skonfiguruj Microsoft repository
# 2. Potem zainstaluj .NET 8
```

### Troubleshooting:

Jeśli nadal masz problemy:

1. **Sprawdź wersję Ubuntu**: `lsb_release -a`
2. **Sprawdź dostępne .NET**: `dotnet --list-sdks`
3. **Uruchom pełną diagnostykę**: `./diagnose.sh`
4. **Spróbuj pełnego setup**: `sudo ./production-setup.sh`

---

📖 **Pełna dokumentacja**: `PRODUCTION-DEPLOYMENT.md`  
🚀 **Szybki start**: `ADMIN-QUICKSTART.md`
