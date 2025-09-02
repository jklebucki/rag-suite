#!/bin/bash

# Docker Compose PostgreSQL Setup for RAG Suite
# Alternative to setup-postgres-docker.sh using docker-compose

set -e

COMPOSE_FILE="scripts/docker-compose.postgres.yml"
DATA_DIR="/opt/rag-suite/postgresql"

echo "🐘 Setting up PostgreSQL with Docker Compose"
echo "============================================="

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
    echo "❌ Docker Compose is not installed. Please install it first."
    echo "   sudo apt install docker-compose"
    exit 1
fi

# Determine docker-compose command
if command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE="docker-compose"
else
    DOCKER_COMPOSE="docker compose"
fi

# Create data directory with proper permissions
echo "📁 Creating data directory: $DATA_DIR"
sudo mkdir -p $DATA_DIR
sudo chown -R 999:999 $DATA_DIR  # PostgreSQL user inside container has UID 999
sudo chmod 755 $DATA_DIR

# Start PostgreSQL with docker-compose
echo "🚀 Starting PostgreSQL with Docker Compose..."
$DOCKER_COMPOSE -f $COMPOSE_FILE up -d

# Wait for PostgreSQL to be ready
echo "⏳ Waiting for PostgreSQL to be ready..."
sleep 15

# Check health status
echo "🔍 Checking PostgreSQL health..."
$DOCKER_COMPOSE -f $COMPOSE_FILE ps

echo ""
echo "🎉 PostgreSQL Setup Complete!"
echo "=============================="
echo "Database: rag-suite"
echo "Username: postgres"
echo "Password: postgres"
echo "Port: 5432"
echo "Data Volume: rag-suite-postgres-data"
echo "Backup Directory: $DATA_DIR"
echo ""
echo "📝 Connection String:"
echo "Host=localhost;Database=rag-suite;Username=postgres;Password=postgres;Port=5432"
echo ""
echo "🔧 Docker Compose Commands:"
echo "============================"
echo "Stop:      $DOCKER_COMPOSE -f $COMPOSE_FILE down"
echo "Start:     $DOCKER_COMPOSE -f $COMPOSE_FILE up -d"
echo "Logs:      $DOCKER_COMPOSE -f $COMPOSE_FILE logs -f postgres"
echo "Status:    $DOCKER_COMPOSE -f $COMPOSE_FILE ps"
echo "Connect:   docker exec -it rag-suite-postgres psql -U postgres -d rag-suite"
