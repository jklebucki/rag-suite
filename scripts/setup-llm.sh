#!/bin/bash

# RAG Suite LLM Setup Script
# This script sets up the LLM environment for the RAG Suite project

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
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

# Check if Docker is running
check_docker() {
    print_status "Checking Docker status..."
    if ! docker info > /dev/null 2>&1; then
        print_error "Docker is not running. Please start Docker and try again."
        exit 1
    fi
    print_success "Docker is running"
}

# Check available disk space (LLM models can be large)
check_disk_space() {
    print_status "Checking available disk space..."
    available_space=$(df -h . | awk 'NR==2 {print $4}' | sed 's/G//')
    if [ "${available_space%.*}" -lt 10 ]; then
        print_warning "Available disk space is less than 10GB. LLM models require significant storage."
        read -p "Do you want to continue? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            exit 1
        fi
    fi
    print_success "Sufficient disk space available"
}

# Pull required Docker images
pull_images() {
    print_status "Pulling required Docker images..."
    
    print_status "Pulling Elasticsearch image..."
    docker pull docker.elastic.co/elasticsearch/elasticsearch:8.11.3
    
    print_status "Pulling Kibana image..."
    docker pull docker.elastic.co/kibana/kibana:8.11.3
    
    print_status "Pulling Text Embeddings Inference image..."
    docker pull ghcr.io/huggingface/text-embeddings-inference:cpu-1.8
    
    print_status "Pulling Text Generation Inference (LLM) image..."
    docker pull ghcr.io/huggingface/text-generation-inference:2.2.0
    
    print_success "All images pulled successfully"
}

# Create necessary directories
create_directories() {
    print_status "Creating necessary directories..."
    
    # Create data directories for persistence
    mkdir -p ../deploy/data/elasticsearch
    mkdir -p ../deploy/data/llm-cache
    
    print_success "Directories created"
}

# Setup environment file
setup_environment() {
    print_status "Setting up environment configuration..."
    
    ENV_FILE="../deploy/.env"
    
    if [ ! -f "$ENV_FILE" ]; then
        # Check if user wants a specific environment
        if [ -f "../deploy/.env.development" ]; then
            print_status "Found development environment template"
            read -p "Use development environment? (Y/n): " -n 1 -r
            echo
            if [[ $REPLY =~ ^[Nn]$ ]]; then
                cp "../deploy/.env.template" "$ENV_FILE"
                print_warning "Using template - please customize $ENV_FILE"
            else
                cp "../deploy/.env.development" "$ENV_FILE"
                print_success "Using development environment configuration"
            fi
        else
            cp "../deploy/.env.template" "$ENV_FILE"
            print_warning "Created $ENV_FILE from template - please customize it"
        fi
        
        print_success "Environment file created at $ENV_FILE"
        print_warning "Review and update the .env file with your specific configuration"
        echo "Available configurations:"
        echo "  • .env.local - Minimal RAM usage (4GB total)"
        echo "  • .env.development - Balanced for development (8GB total)"  
        echo "  • .env - Default balanced setup (9GB total)"
        echo "  • .env.production - Full performance (14GB total)"
    else
        print_success "Environment file already exists"
        
        # Show current model and memory settings
        if grep -q "LLM_MODEL_ID" "$ENV_FILE"; then
            model=$(grep "LLM_MODEL_ID" "$ENV_FILE" | cut -d'=' -f2)
            memory=$(grep "LLM_MAX_MEMORY" "$ENV_FILE" | cut -d'=' -f2 || echo "not set")
            es_memory=$(grep "ES_JAVA_OPTS" "$ENV_FILE" | cut -d'=' -f2 || echo "not set")
            print_status "Current configuration:"
            echo "  • LLM Model: $model"
            echo "  • LLM Memory: $memory" 
            echo "  • Elasticsearch Memory: $es_memory"
        fi
    fi
}

# Start services
start_services() {
    print_status "Starting RAG Suite services..."
    
    cd ../deploy
    
    # Start services in order
    print_status "Starting Elasticsearch..."
    docker-compose up -d elasticsearch
    
    print_status "Waiting for Elasticsearch to be ready..."
    sleep 30
    
    print_status "Starting Kibana..."
    docker-compose up -d kibana
    
    print_status "Starting Embedding Service..."
    docker-compose up -d embedding-service
    
    print_status "Starting LLM Service (this may take a while to download the model)..."
    docker-compose up -d llm-service
    
    print_success "All services started"
    
    # Wait for services to be ready
    print_status "Waiting for services to be ready..."
    sleep 60
    
    # Check service health
    check_services_health
}

# Check services health
check_services_health() {
    print_status "Checking services health..."
    
    # Check Elasticsearch
    if curl -s -u elastic:elastic http://localhost:9200/_cluster/health > /dev/null; then
        print_success "Elasticsearch is healthy"
    else
        print_warning "Elasticsearch may not be ready yet"
    fi
    
    # Check Embedding Service
    if curl -s http://localhost:8580/health > /dev/null; then
        print_success "Embedding Service is healthy"
    else
        print_warning "Embedding Service may not be ready yet"
    fi
    
    # Check LLM Service
    if curl -s http://localhost:8581/health > /dev/null; then
        print_success "LLM Service is healthy"
    else
        print_warning "LLM Service may not be ready yet (model might still be downloading)"
    fi
}

# Main execution
main() {
    echo -e "${BLUE}"
    echo "================================================="
    echo "        RAG Suite LLM Setup Script"
    echo "================================================="
    echo -e "${NC}"
    
    check_docker
    check_disk_space
    create_directories
    setup_environment
    pull_images
    start_services
    
    echo
    echo -e "${GREEN}"
    echo "================================================="
    echo "              Setup Complete!"
    echo "================================================="
    echo -e "${NC}"
    echo
    echo "Services are now running:"
    echo "  • Elasticsearch: http://localhost:9200"
    echo "  • Kibana: http://localhost:5601"
    echo "  • Embedding Service: http://localhost:8580"
    echo "  • LLM Service: http://localhost:8581"
    echo
    echo "Next steps:"
    echo "  1. Review the .env file in the deploy directory"
    echo "  2. Build and run the RAG.Orchestrator.Api"
    echo "  3. Start the RAG.Web.UI"
    echo "  4. Test the chat functionality"
    echo
    echo "For monitoring, check: docker-compose logs -f"
}

# Run the main function
main "$@"
