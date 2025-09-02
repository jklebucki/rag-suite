#!/bin/bash

# PostgreSQL Docker Container Setup Script for RAG Suite
# This script creates and runs a PostgreSQL container with intelligent directory handling
# It automatically handles filesystem permission issues and read-only systems

set -e

# Configuration
CONTAINER_NAME="rag-suite-postgres"
POSTGRES_VERSION="15-alpine"
POSTGRES_DB="rag-suite"
POSTGRES_USER="postgres"
POSTGRES_PASSWORD="postgres"
POSTGRES_PORT="5432"
VOLUME_NAME="rag-suite-postgres-data"

echo "🐘 Setting up PostgreSQL container for RAG Suite"
echo "================================================="

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

# Function to test directory creation
test_directory_creation() {
    local test_dir="$1"
    local parent_dir=$(dirname "$test_dir")
    
    echo "   Testing: $test_dir"
    
    # Try to create parent directory
    if sudo mkdir -p "$parent_dir" 2>/dev/null; then
        if sudo mkdir -p "$test_dir" 2>/dev/null; then
            if sudo chown -R 999:999 "$test_dir" 2>/dev/null; then
                sudo chmod 755 "$test_dir" 2>/dev/null
                echo "   ✅ Successfully created: $test_dir"
                return 0
            fi
        fi
    fi
    echo "   ❌ Cannot create/access: $test_dir"
    return 1
}

# Try to find suitable directory for backup mount
echo "🔍 Finding suitable location for PostgreSQL backup directory..."

# Preferred locations in order
PREFERRED_LOCATIONS=(
    "/var/lib/rag-suite/postgresql"
    "/home/docker-data/rag-suite/postgresql"
    "/tmp/rag-suite/postgresql"
)

DATA_DIR=""
USE_HOST_MOUNT=false

for location in "${PREFERRED_LOCATIONS[@]}"; do
    if test_directory_creation "$location"; then
        DATA_DIR="$location"
        USE_HOST_MOUNT=true
        echo "✅ Will use backup directory: $DATA_DIR"
        break
    fi
done

if [ "$USE_HOST_MOUNT" = false ]; then
    echo ""
    echo "⚠️  Cannot create host directory for PostgreSQL backup"
    echo "💡 Using Docker volume-only approach (recommended for read-only systems)"
    echo "   Data will be stored securely in Docker's internal volume system"
    echo ""
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

# Prepare Docker run command based on available directory
if [ "$USE_HOST_MOUNT" = true ]; then
    echo "   Using host directory mount for backups: $DATA_DIR"
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
        -v $DATA_DIR:/backup \
        --shm-size=256mb \
        postgres:$POSTGRES_VERSION
else
    echo "   Using Docker volume-only approach"
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
fi

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
sleep 5
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

if [ "$USE_HOST_MOUNT" = true ]; then
    echo "Backup Directory: $DATA_DIR"
else
    echo "Backup: Use 'docker exec' commands (see below)"
fi

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

if [ "$USE_HOST_MOUNT" = true ]; then
    echo "Backup database:    docker exec $CONTAINER_NAME pg_dump -U $POSTGRES_USER $POSTGRES_DB > $DATA_DIR/backup_\$(date +%Y%m%d_%H%M%S).sql"
else
    echo "Backup database:    docker exec $CONTAINER_NAME pg_dump -U $POSTGRES_USER $POSTGRES_DB > backup_\$(date +%Y%m%d_%H%M%S).sql"
fi

echo "Volume location:    docker volume inspect $VOLUME_NAME"
echo ""
echo "🔄 Auto-restart: Container will automatically restart after server reboot"
echo "📊 Monitor status: docker ps | grep $CONTAINER_NAME"

if [ "$USE_HOST_MOUNT" = false ]; then
    echo ""
    echo "💡 Note: Running in volume-only mode (no filesystem mount issues)"
    echo "   This is perfect for read-only filesystems and secure environments"
fi
