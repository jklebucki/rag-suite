#!/bin/bash

# PostgreSQL Setup Diagnostics for RAG Suite
# This script helps diagnose filesystem and Docker issues

echo "🔍 PostgreSQL Setup Diagnostics"
echo "==============================="

# Check Docker installation
echo "📦 Docker Status:"
if command -v docker &> /dev/null; then
    echo "✅ Docker is installed: $(docker --version)"
    if docker info &> /dev/null; then
        echo "✅ Docker daemon is running"
    else
        echo "❌ Docker daemon is not running"
        echo "   Try: sudo systemctl start docker"
    fi
else
    echo "❌ Docker is not installed"
    echo "   Install: sudo apt update && sudo apt install docker.io"
fi

echo ""

# Check filesystem permissions
echo "📁 Filesystem Permissions:"

# Test /opt
if [ -w /opt ]; then
    echo "✅ /opt is writable"
    OPT_AVAILABLE=true
else
    echo "❌ /opt is not writable (read-only filesystem?)"
    OPT_AVAILABLE=false
fi

# Test /var/lib
if [ -w /var/lib ]; then
    echo "✅ /var/lib is writable"
    VAR_LIB_AVAILABLE=true
else
    echo "❌ /var/lib is not writable"
    VAR_LIB_AVAILABLE=false
fi

# Test current directory
if [ -w . ]; then
    echo "✅ Current directory is writable"
    CURRENT_DIR_AVAILABLE=true
else
    echo "❌ Current directory is not writable"
    CURRENT_DIR_AVAILABLE=false
fi

echo ""

# Recommendation
echo "💡 Recommendations:"
echo "==================="

if [ "$OPT_AVAILABLE" = true ]; then
    echo "✅ Use: ./setup-postgres-docker.sh (with /var/lib mount)"
elif [ "$VAR_LIB_AVAILABLE" = true ]; then
    echo "✅ Use: ./setup-postgres-docker.sh (with /var/lib mount)"
else
    echo "⚠️  Use: ./setup-postgres-volume-only.sh (Docker volumes only)"
    echo "   This works on read-only filesystems"
fi

echo ""

# Check if container already exists
echo "📋 Existing PostgreSQL Containers:"
if docker ps -a --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}' | grep -q "rag-suite-postgres"; then
    docker ps -a --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}' | head -1
    docker ps -a --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}' | grep "rag-suite-postgres"
else
    echo "No existing rag-suite-postgres container found"
fi

echo ""

# Check volumes
echo "📦 Existing PostgreSQL Volumes:"
if docker volume ls | grep -q "rag-suite-postgres"; then
    docker volume ls | head -1
    docker volume ls | grep "rag-suite-postgres"
else
    echo "No existing rag-suite-postgres volumes found"
fi

echo ""
echo "🚀 Next Steps:"
echo "=============="
echo "1. If filesystem is writable: ./setup-postgres-docker.sh"
echo "2. If filesystem is read-only: ./setup-postgres-volume-only.sh"
echo "3. Alternative: ./setup-postgres-compose.sh"
echo "4. Check status: ./postgres-manager.sh status"
