#!/bin/bash

# RAG Suite Environment Manager
# Comprehensive script for managing the entire RAG Suite environment

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

show_help() {
    echo "RAG Suite Environment Manager"
    echo
    echo "Usage: $0 [COMMAND]"
    echo
    echo "Commands:"
    echo "  setup         Setup entire environment from scratch"
    echo "  start         Start all services"
    echo "  stop          Stop all services"
    echo "  restart       Restart all services"
    echo "  status        Show status of all services"
    echo "  logs          Show logs from all services"
    echo "  logs [SERVICE] Show logs from specific service"
    echo "  test          Run integration tests"
    echo "  clean         Clean up all containers and volumes"
    echo "  build-api     Build and run the Orchestrator API"
    echo "  build-ui      Build and run the Web UI"
    echo "  monitor       Start monitoring dashboard"
    echo "  help          Show this help message"
    echo
    echo "Services: elasticsearch, kibana, embedding-service, llm-service"
}

setup_environment() {
    print_status "Setting up complete RAG Suite environment..."
    
    # Run LLM setup script
    if [ -f "./setup-llm.sh" ]; then
        print_status "Running LLM setup..."
        ./setup-llm.sh
    else
        print_error "setup-llm.sh not found. Please run from scripts directory."
        exit 1
    fi
    
    print_success "Environment setup completed!"
}

start_services() {
    print_status "Starting RAG Suite services..."
    cd ../deploy
    
    # Start in order
    print_status "Starting Elasticsearch..."
    docker-compose up -d elasticsearch
    sleep 20
    
    print_status "Starting Kibana..."
    docker-compose up -d kibana
    
    print_status "Starting Embedding Service..."
    docker-compose up -d embedding-service
    
    print_status "Starting LLM Service..."
    docker-compose up -d llm-service
    
    print_success "All services started!"
    show_status
}

stop_services() {
    print_status "Stopping RAG Suite services..."
    cd ../deploy
    docker-compose down
    print_success "All services stopped!"
}

restart_services() {
    print_status "Restarting RAG Suite services..."
    stop_services
    sleep 5
    start_services
}

show_status() {
    print_status "RAG Suite Services Status:"
    cd ../deploy
    
    echo
    echo "Container Status:"
    docker-compose ps
    
    echo
    echo "Service Health Checks:"
    
    # Elasticsearch
    if curl -s -u elastic:changeme http://localhost:9200/_cluster/health > /dev/null 2>&1; then
        echo -e "  Elasticsearch: ${GREEN}✓ Healthy${NC}"
    else
        echo -e "  Elasticsearch: ${RED}✗ Not responding${NC}"
    fi
    
    # Kibana
    if curl -s http://localhost:5601 > /dev/null 2>&1; then
        echo -e "  Kibana: ${GREEN}✓ Healthy${NC}"
    else
        echo -e "  Kibana: ${YELLOW}⚠ Not ready${NC}"
    fi
    
    # Embedding Service
    if curl -s http://localhost:8580/health > /dev/null 2>&1; then
        echo -e "  Embedding Service: ${GREEN}✓ Healthy${NC}"
    else
        echo -e "  Embedding Service: ${YELLOW}⚠ Not ready${NC}"
    fi
    
    # LLM Service
    if curl -s http://localhost:8581/health > /dev/null 2>&1; then
        echo -e "  LLM Service: ${GREEN}✓ Healthy${NC}"
    else
        echo -e "  LLM Service: ${YELLOW}⚠ Not ready${NC}"
    fi
    
    # Check if Orchestrator API is running
    if curl -s http://localhost:7107/health > /dev/null 2>&1; then
        echo -e "  Orchestrator API: ${GREEN}✓ Running${NC}"
    else
        echo -e "  Orchestrator API: ${YELLOW}⚠ Not running${NC}"
    fi
    
    echo
    echo "Access URLs:"
    echo "  • Elasticsearch: http://localhost:9200"
    echo "  • Kibana: http://localhost:5601"
    echo "  • Embedding Service: http://localhost:8580"
    echo "  • LLM Service: http://localhost:8581"
    echo "  • Orchestrator API: http://localhost:7107"
    echo "  • Web UI: http://localhost:5173 (if running)"
}

show_logs() {
    cd ../deploy
    
    if [ -z "$1" ]; then
        print_status "Showing logs from all services..."
        docker-compose logs -f --tail=50
    else
        print_status "Showing logs from $1..."
        docker-compose logs -f --tail=100 "$1"
    fi
}

run_tests() {
    print_status "Running integration tests..."
    
    if [ -f "./test-llm-integration.sh" ]; then
        ./test-llm-integration.sh
    else
        print_error "test-llm-integration.sh not found. Please run from scripts directory."
        exit 1
    fi
}

clean_environment() {
    print_warning "This will remove all containers, volumes, and data!"
    read -p "Are you sure? (y/N): " -n 1 -r
    echo
    
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_status "Cleaning up environment..."
        cd ../deploy
        
        docker-compose down -v --remove-orphans
        docker volume prune -f
        
        print_success "Environment cleaned!"
    else
        print_status "Clean operation cancelled."
    fi
}

build_api() {
    print_status "Building and running Orchestrator API..."
    cd ../src/RAG.Orchestrator.Api
    
    # Restore dependencies
    print_status "Restoring NuGet packages..."
    dotnet restore
    
    # Build
    print_status "Building project..."
    dotnet build
    
    # Run in background
    print_status "Starting API server..."
    print_warning "API will run in background. Use 'pkill -f dotnet' to stop."
    nohup dotnet run > /tmp/rag-api.log 2>&1 &
    
    sleep 5
    
    # Check if running
    if curl -s http://localhost:7107/health > /dev/null; then
        print_success "API is running at http://localhost:7107"
        echo "Swagger UI: http://localhost:7107"
    else
        print_error "Failed to start API. Check logs: tail -f /tmp/rag-api.log"
    fi
}

build_ui() {
    print_status "Building and running Web UI..."
    cd ../src/RAG.Web.UI
    
    # Install dependencies
    if [ ! -d "node_modules" ]; then
        print_status "Installing npm dependencies..."
        npm install
    fi
    
    # Start development server
    print_status "Starting development server..."
    print_warning "UI will run in background. Use 'pkill -f vite' to stop."
    nohup npm run dev > /tmp/rag-ui.log 2>&1 &
    
    sleep 5
    print_success "Web UI should be running at http://localhost:5173"
    echo "Check logs: tail -f /tmp/rag-ui.log"
}

monitor_resources() {
    print_status "Starting resource monitoring..."
    
    echo "Press Ctrl+C to stop monitoring"
    echo
    
    while true; do
        clear
        echo "RAG Suite Resource Monitor - $(date)"
        echo "=================================="
        echo
        
        echo "Container Status:"
        cd ../deploy
        docker-compose ps
        
        echo
        echo "Resource Usage:"
        docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}" \
            es kibana embedding-service llm-service 2>/dev/null || echo "Some containers not running"
        
        echo
        echo "System Resources:"
        echo "Memory: $(free -h 2>/dev/null | grep '^Mem:' || echo 'N/A on macOS')"
        echo "Disk: $(df -h . | tail -1)"
        
        sleep 5
    done
}

# Main script logic
case "$1" in
    setup)
        setup_environment
        ;;
    start)
        start_services
        ;;
    stop)
        stop_services
        ;;
    restart)
        restart_services
        ;;
    status)
        show_status
        ;;
    logs)
        show_logs "$2"
        ;;
    test)
        run_tests
        ;;
    clean)
        clean_environment
        ;;
    build-api)
        build_api
        ;;
    build-ui)
        build_ui
        ;;
    monitor)
        monitor_resources
        ;;
    help|--help|-h)
        show_help
        ;;
    "")
        show_help
        ;;
    *)
        print_error "Unknown command: $1"
        echo
        show_help
        exit 1
        ;;
esac
