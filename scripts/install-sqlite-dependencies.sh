#!/bin/bash

# Script to install SQLite dependencies on Linux server
# This script should be run on the production server to resolve SQLite issues

set -e

echo "🔧 Installing SQLite dependencies for RAG Suite..."

# Detect Linux distribution
if [ -f /etc/os-release ]; then
    . /etc/os-release
    OS=$NAME
    VER=$VERSION_ID
else
    echo "Cannot detect Linux distribution"
    exit 1
fi

echo "Detected OS: $OS $VER"

# Install SQLite based on distribution
case $OS in
    "Ubuntu"*)
        echo "Installing SQLite for Ubuntu..."
        sudo apt-get update
        sudo apt-get install -y sqlite3 libsqlite3-dev
        ;;
    "Debian"*)
        echo "Installing SQLite for Debian..."
        sudo apt-get update
        sudo apt-get install -y sqlite3 libsqlite3-dev
        ;;
    "CentOS"*|"Red Hat"*|"Rocky"*|"AlmaLinux"*)
        echo "Installing SQLite for RHEL-based distribution..."
        sudo yum install -y sqlite sqlite-devel
        ;;
    "Fedora"*)
        echo "Installing SQLite for Fedora..."
        sudo dnf install -y sqlite sqlite-devel
        ;;
    "SUSE"*|"openSUSE"*)
        echo "Installing SQLite for SUSE..."
        sudo zypper install -y sqlite3 sqlite3-devel
        ;;
    *)
        echo "⚠️  Unsupported distribution: $OS"
        echo "Please install sqlite3 and sqlite3-dev packages manually"
        exit 1
        ;;
esac

# Verify installation
echo "📋 Verifying SQLite installation..."
sqlite3 --version

# Check if libraries are available
echo "📋 Checking available SQLite libraries..."
find /usr/lib* -name "*sqlite*" -type f 2>/dev/null | head -10 || echo "No SQLite libraries found in standard locations"

# Check glibc version
echo "📋 Checking GLIBC version..."
ldd --version | head -1

echo "✅ SQLite dependencies installation completed!"
echo ""
echo "📝 Next steps:"
echo "1. Restart the RAG API service: sudo systemctl restart rag-api"
echo "2. Check service status: sudo systemctl status rag-api"
echo "3. Check logs: sudo journalctl -u rag-api -f"
