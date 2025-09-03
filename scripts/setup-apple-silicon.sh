#!/bin/bash

# RAG Suite Apple Silicon / ARM64 Setup Script
# Specialized script for Apple Silicon Macs

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

# Detect architecture
detect_architecture() {
    arch=$(uname -m)
    if [[ "$arch" == "arm64" ]]; then
        print_status "Detected Apple Silicon (ARM64) architecture"
        return 0
    else
        print_status "Detected x86_64 architecture"
        return 1
    fi
}

# Install Ollama if not present
install_ollama() {
    if ! command -v ollama &> /dev/null; then
        print_status "Installing Ollama..."
        curl -fsSL https://ollama.ai/install.sh | sh
        print_success "Ollama installed"
    else
        print_success "Ollama already installed"
    fi
}

# Setup Ollama models
setup_ollama_models() {
    print_status "Setting up Ollama models..."
    
    # Start Ollama service if not running
    if ! pgrep -f ollama > /dev/null; then
        print_status "Starting Ollama service..."
        ollama serve &
        sleep 5
    fi
    
    # Pull a lightweight chat model
    print_status "Downloading chat model (this may take a while)..."
    ollama pull llama3.2:1b
    
    print_success "Ollama models ready"
}

# Start services with Apple Silicon configuration
start_apple_silicon_services() {
    print_status "Starting RAG Suite with Apple Silicon configuration..."
    cd ../deploy
    
    # Use Apple Silicon docker-compose file
    docker-compose -f docker-compose.apple-silicon.yml up -d
    
    print_status "Waiting for services to start..."
    sleep 30
    
    # Check service health
    check_services_health
}

# Check services health for Apple Silicon setup
check_services_health() {
    print_status "Checking services health..."
    
    # Check Elasticsearch
    if curl -s -u elastic:elastic http://localhost:9200/_cluster/health > /dev/null; then
        print_success "Elasticsearch is healthy"
    else
        print_warning "Elasticsearch may not be ready yet"
    fi
    
    # Check Ollama
    if curl -s http://localhost:11434/api/tags > /dev/null; then
        print_success "Ollama is healthy"
    else
        print_warning "Ollama may not be ready yet"
    fi
    
    # Check Embedding Service
    if curl -s http://localhost:8580/health > /dev/null; then
        print_success "Embedding Service is healthy"
    else
        print_warning "Embedding Service may not be ready yet"
    fi
}

# Update ChatService to use Ollama
update_chat_service_for_ollama() {
    print_status "Note: You'll need to update ChatService.cs to use Ollama API"
    print_warning "The LlmService should connect to http://localhost:11434/api/generate"
    print_warning "See Ollama API documentation: https://github.com/ollama/ollama/blob/main/docs/api.md"
}

# Main execution
main() {
    echo -e "${BLUE}"
    echo "================================================="
    echo "     RAG Suite Apple Silicon Setup"
    echo "================================================="
    echo -e "${NC}"
    
    if detect_architecture; then
        print_status "Setting up RAG Suite for Apple Silicon..."
        
        install_ollama
        setup_ollama_models
        start_apple_silicon_services
        update_chat_service_for_ollama
        
        echo
        echo -e "${GREEN}"
        echo "================================================="
        echo "         Apple Silicon Setup Complete!"
        echo "================================================="
        echo -e "${NC}"
        echo
        echo "Services running:"
        echo "  • Elasticsearch: http://localhost:9200"
        echo "  • Kibana: http://localhost:5601"
        echo "  • Ollama (LLM): http://localhost:11434"
        echo "  • Embedding Service: http://localhost:8580"
        echo
        echo "Next steps:"
        echo "  1. Update ChatService.cs to use Ollama API"
        echo "  2. Test with: curl http://localhost:11434/api/generate"
        echo "  3. Build and run the Orchestrator API"
        
    else
        print_error "This script is designed for Apple Silicon Macs"
        print_status "Use the regular setup script for x86_64 systems"
    fi
}

# Run the main function
main "$@"
