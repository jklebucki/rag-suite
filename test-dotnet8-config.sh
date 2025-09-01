#!/bin/bash

# Test script dla sprawdzenia Ubuntu version detection
# i konfiguracji .NET 8

echo "=== .NET 8 Configuration Test ==="
echo

# Sprawdź wersję Ubuntu
if command -v lsb_release &> /dev/null; then
    UBUNTU_VERSION=$(lsb_release -rs)
    UBUNTU_CODENAME=$(lsb_release -cs)
    echo "Ubuntu Version: $UBUNTU_VERSION ($UBUNTU_CODENAME)"
else
    echo "lsb_release not found - cannot detect Ubuntu version"
    exit 1
fi

echo
echo "=== Repository Strategy ==="

case $UBUNTU_VERSION in
    "18.04"|"20.04")
        echo "✅ Ubuntu $UBUNTU_VERSION detected"
        echo "📋 Strategy: Microsoft Package Repository (REQUIRED)"
        echo "🔗 Repository URL: https://packages.microsoft.com/config/ubuntu/$UBUNTU_VERSION/prod.list"
        echo "⚠️  Note: Ubuntu feeds DO NOT contain .NET 8 for this version"
        ;;
    "22.04")
        echo "✅ Ubuntu $UBUNTU_VERSION detected"
        echo "📋 Strategy: Microsoft Package Repository (Recommended)"
        echo "🔗 Repository URL: https://packages.microsoft.com/config/ubuntu/$UBUNTU_VERSION/prod.list"
        echo "ℹ️  Note: Ubuntu feeds available but Microsoft repo preferred for .NET 8"
        ;;
    "24.04"|"25.04")
        echo "✅ Ubuntu $UBUNTU_VERSION detected"
        echo "📋 Strategy: Ubuntu Built-in or Backports"
        echo "🔗 Built-in: apt install dotnet-sdk-8.0"
        echo "🔗 Backports: ppa:dotnet/backports"
        echo "⚠️  Note: Microsoft repo not supported for Ubuntu 24.04+"
        ;;
    *)
        echo "❓ Unknown Ubuntu version: $UBUNTU_VERSION"
        echo "📋 Strategy: .NET Install Script (Fallback)"
        echo "🔗 Script: https://dot.net/v1/dotnet-install.sh"
        ;;
esac

echo
echo "=== Current .NET Status ==="

if command -v dotnet &> /dev/null; then
    echo "✅ .NET is installed"
    echo "Version: $(dotnet --version)"
    echo
    echo "Installed SDKs:"
    dotnet --list-sdks
    echo
    echo "Installed Runtimes:"
    dotnet --list-runtimes
    echo
    
    if dotnet --list-sdks | grep -q "8\.0\."; then
        echo "✅ .NET 8 SDK is available!"
        echo "Status: READY FOR COMPILATION"
    else
        echo "❌ .NET 8 SDK is NOT available"
        echo "Status: NEEDS .NET 8 INSTALLATION"
    fi
else
    echo "❌ .NET is not installed"
    echo "Status: NEEDS FULL .NET INSTALLATION"
fi

echo
echo "=== Recommendations ==="

if command -v dotnet &> /dev/null && dotnet --list-sdks | grep -q "8\.0\."; then
    echo "🎉 Your system is ready!"
    echo "✅ Run: cd /var/www/rag-suite && dotnet build src/RAG.Orchestrator.Api/RAG.Orchestrator.Api.csproj"
else
    echo "🔧 Action needed:"
    echo "✅ Run: sudo ./install-dotnet8.sh"
    echo "✅ Then: cd /var/www/rag-suite && sudo ./deploy.sh"
fi

echo
echo "=== Test Completed ==="
