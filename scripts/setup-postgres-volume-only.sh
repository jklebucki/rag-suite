#!/bin/bash

# PostgreSQL Docker Container Setup Script for RAG Suite (Volume-only version)
# This script creates and runs a PostgreSQL container using only Docker volumes
# For systems where filesystem is read-only or mount points are restricted

set -e

# Configuration
CONTAINER_NAME="rag-suite-postgres"
POSTGRES_VERSION="15-alpine"
POSTGRES_DB="rag-suite"
POSTGRES_USER="postgres"
POSTGRES_PASSWORD="postgres"
POSTGRES_PORT="5432"
VOLUME_NAME="rag-suite-postgres-data"

echo "🐘 Setting up PostgreSQL container for RAG Suite (Volume-only)"
echo "=============================================================="

# Check if Docker is installed and running
if ! command -v docker &> /dev/null; then
    echo "❌ Docker is not installed. Please install Docker first."
    echo "   sudo apt update && sudo apt install docker.io"
    exit 1
fi

if ! docker info &> /dev/null; then
    echo "❌ Docker is not running. Please start Docker service."
    echo "   sudo systemctl start docker"
    exit 1
fi

# Stop and remove existing container if it exists
if docker ps -a --format 'table {{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
    echo "🛑 Stopping existing container: $CONTAINER_NAME"
    docker stop $CONTAINER_NAME 2>/dev/null || true
    echo "🗑️  Removing existing container: $CONTAINER_NAME"
    docker rm $CONTAINER_NAME 2>/dev/null || true
fi

# Create Docker volume if it doesn't exist
if ! docker volume ls | grep -q "$VOLUME_NAME"; then
    echo "📦 Creating Docker volume: $VOLUME_NAME"
    docker volume create $VOLUME_NAME
fi

echo "🚀 Starting PostgreSQL container..."

# Run PostgreSQL container with only Docker volume (no host mount)
docker run -d \
    --name $CONTAINER_NAME \
    --restart unless-stopped \
    -e POSTGRES_DB=$POSTGRES_DB \
    -e POSTGRES_USER=$POSTGRES_USER \
    -e POSTGRES_PASSWORD=$POSTGRES_PASSWORD \
    -e POSTGRES_INITDB_ARGS="--encoding=UTF-8 --lc-collate=C --lc-ctype=C" \
    -e PGDATA=/var/lib/postgresql/data/pgdata \
    -p $POSTGRES_PORT:5432 \
    -v $VOLUME_NAME:/var/lib/postgresql/data \
    --shm-size=256mb \
    postgres:$POSTGRES_VERSION

# Wait for PostgreSQL to be ready
echo "⏳ Waiting for PostgreSQL to be ready..."
sleep 10

# Check if container is running
if docker ps --format 'table {{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
    echo "✅ PostgreSQL container started successfully!"
else
    echo "❌ Failed to start PostgreSQL container"
    echo "📋 Container logs:"
    docker logs $CONTAINER_NAME
    exit 1
fi

# Test connection
echo "🔍 Testing PostgreSQL connection..."
if docker exec $CONTAINER_NAME pg_isready -U $POSTGRES_USER -d $POSTGRES_DB &> /dev/null; then
    echo "✅ PostgreSQL is ready and accepting connections"
else
    echo "⚠️  PostgreSQL is starting but not yet ready for connections"
    echo "   It may take a few more seconds to initialize"
fi

echo ""
echo "🎉 PostgreSQL Setup Complete!"
echo "=============================="
echo "Container Name: $CONTAINER_NAME"
echo "Database: $POSTGRES_DB"
echo "Username: $POSTGRES_USER"
echo "Password: $POSTGRES_PASSWORD"
echo "Port: $POSTGRES_PORT"
echo "Data Volume: $VOLUME_NAME (Docker managed)"
echo ""
echo "📝 Connection String:"
echo "Host=localhost;Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD;Port=$POSTGRES_PORT"
echo ""
echo "🔧 Useful Commands:"
echo "==================="
echo "Stop container:     docker stop $CONTAINER_NAME"
echo "Start container:    docker start $CONTAINER_NAME"
echo "View logs:          docker logs $CONTAINER_NAME -f"
echo "Connect to DB:      docker exec -it $CONTAINER_NAME psql -U $POSTGRES_USER -d $POSTGRES_DB"
echo "Backup database:    docker exec $CONTAINER_NAME pg_dump -U $POSTGRES_USER $POSTGRES_DB > backup_\$(date +%Y%m%d_%H%M%S).sql"
echo "Volume location:    docker volume inspect $VOLUME_NAME"
echo ""
echo "🔄 Auto-restart: Container will automatically restart after server reboot"
echo "📊 Monitor status: docker ps | grep $CONTAINER_NAME"
echo ""
echo "💡 Note: This setup uses only Docker volumes (no host filesystem mounts)"
echo "   Data is stored in Docker's internal volume system"
