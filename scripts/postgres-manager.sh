#!/bin/bash

# PostgreSQL Container Management Script for RAG Suite
# Usage: ./postgres-manager.sh [start|stop|restart|status|logs|backup|connect|remove]

set -e

CONTAINER_NAME="rag-suite-postgres"
POSTGRES_DB="rag-suite"
POSTGRES_USER="postgres"
DATA_DIR="/opt/rag-suite/postgresql"

case "${1:-status}" in
    "start")
        echo "🚀 Starting PostgreSQL container..."
        docker start $CONTAINER_NAME
        echo "✅ Container started"
        ;;
        
    "stop")
        echo "🛑 Stopping PostgreSQL container..."
        docker stop $CONTAINER_NAME
        echo "✅ Container stopped"
        ;;
        
    "restart")
        echo "🔄 Restarting PostgreSQL container..."
        docker restart $CONTAINER_NAME
        echo "✅ Container restarted"
        ;;
        
    "status")
        echo "📊 PostgreSQL Container Status:"
        echo "==============================="
        if docker ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}' | grep -q "$CONTAINER_NAME"; then
            docker ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}' | grep "$CONTAINER_NAME"
            echo ""
            echo "🔍 Connection test:"
            if docker exec $CONTAINER_NAME pg_isready -U $POSTGRES_USER -d $POSTGRES_DB &> /dev/null; then
                echo "✅ PostgreSQL is ready and accepting connections"
            else
                echo "❌ PostgreSQL is not ready"
            fi
        else
            echo "❌ Container is not running"
        fi
        ;;
        
    "logs")
        echo "📋 PostgreSQL Container Logs:"
        echo "============================="
        docker logs $CONTAINER_NAME -f
        ;;
        
    "backup")
        BACKUP_FILE="$DATA_DIR/backup_$(date +%Y%m%d_%H%M%S).sql"
        echo "💾 Creating database backup..."
        echo "Backup file: $BACKUP_FILE"
        docker exec $CONTAINER_NAME pg_dump -U $POSTGRES_USER $POSTGRES_DB > "$BACKUP_FILE"
        echo "✅ Backup created successfully"
        echo "📁 File size: $(du -h "$BACKUP_FILE" | cut -f1)"
        ;;
        
    "connect")
        echo "🔗 Connecting to PostgreSQL database..."
        echo "Use \\q to exit"
        docker exec -it $CONTAINER_NAME psql -U $POSTGRES_USER -d $POSTGRES_DB
        ;;
        
    "remove")
        read -p "⚠️  This will permanently remove the container (data will be preserved in volume). Continue? (y/N): " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            echo "🛑 Stopping container..."
            docker stop $CONTAINER_NAME 2>/dev/null || true
            echo "🗑️  Removing container..."
            docker rm $CONTAINER_NAME
            echo "✅ Container removed (volume preserved)"
        else
            echo "❌ Operation cancelled"
        fi
        ;;
        
    "help"|"-h"|"--help")
        echo "PostgreSQL Container Manager for RAG Suite"
        echo "=========================================="
        echo ""
        echo "Usage: $0 [command]"
        echo ""
        echo "Commands:"
        echo "  start     - Start the PostgreSQL container"
        echo "  stop      - Stop the PostgreSQL container"  
        echo "  restart   - Restart the PostgreSQL container"
        echo "  status    - Show container status and connection test"
        echo "  logs      - Show container logs (follow mode)"
        echo "  backup    - Create database backup"
        echo "  connect   - Connect to PostgreSQL database"
        echo "  remove    - Remove container (preserves data volume)"
        echo "  help      - Show this help message"
        echo ""
        echo "Examples:"
        echo "  $0 status          # Check if PostgreSQL is running"
        echo "  $0 backup          # Create a database backup"
        echo "  $0 connect         # Connect to database"
        ;;
        
    *)
        echo "❌ Unknown command: $1"
        echo "Use '$0 help' for available commands"
        exit 1
        ;;
esac
