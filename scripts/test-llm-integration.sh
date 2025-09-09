#!/bin/bash

# RAG Suite LLM Integration Test Script
# This script tests the integration between components

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

# Test service endpoints
test_services() {
    print_status "Testing service endpoints..."
    
    # Test Elasticsearch
    print_status "Testing Elasticsearch..."
    if curl -s -u elastic:elastic http://localhost:9200/_cluster/health | grep -q "green\|yellow"; then
        print_success "Elasticsearch is responding"
    else
        print_error "Elasticsearch is not responding"
        return 1
    fi
    
    # Test Embedding Service
    print_status "Testing Embedding Service..."
    if curl -s http://192.168.21.14:8580/health > /dev/null; then
        print_success "Embedding Service is responding"
    else
        print_warning "Embedding Service may not be ready"
    fi
    
    # Test LLM Service
    print_status "Testing LLM Service..."
    if curl -s http://localhost:8581/health > /dev/null; then
        print_success "LLM Service is responding"
    else
        print_warning "LLM Service may not be ready (model might still be downloading)"
    fi
    
    # Test LLM generation
    print_status "Testing LLM text generation..."
    response=$(curl -s -X POST http://localhost:8581/generate \
        -H "Content-Type: application/json" \
        -d '{
            "inputs": "Witaj! Jak się masz?",
            "parameters": {
                "max_new_tokens": 50,
                "temperature": 0.7,
                "do_sample": true,
                "return_full_text": false
            }
        }' || echo "failed")
    
    if [ "$response" != "failed" ] && echo "$response" | grep -q "generated_text"; then
        print_success "LLM text generation working"
        echo "Sample response: $(echo "$response" | jq -r '.generated_text' 2>/dev/null || echo "$response")"
    else
        print_warning "LLM text generation may have issues"
    fi
}

# Test Orchestrator API (if running)
test_orchestrator_api() {
    print_status "Testing Orchestrator API..."
    
    # Check if API is running
    if curl -s http://localhost:7107/health > /dev/null; then
        print_success "Orchestrator API is responding"
        
        # Test chat health endpoint
        if curl -s http://localhost:7107/api/chat/health > /dev/null; then
            print_success "Chat service is available"
        else
            print_warning "Chat service may have issues"
        fi
        
        # Create a test chat session
        session_response=$(curl -s -X POST http://localhost:7107/api/chat/sessions \
            -H "Content-Type: application/json" \
            -d '{"title": "Test Session"}' || echo "failed")
        
        if [ "$session_response" != "failed" ]; then
            session_id=$(echo "$session_response" | jq -r '.data.id' 2>/dev/null)
            if [ "$session_id" != "null" ] && [ "$session_id" != "" ]; then
                print_success "Created test chat session: $session_id"
                
                # Send a test message
                message_response=$(curl -s -X POST "http://localhost:7107/api/chat/sessions/$session_id/messages" \
                    -H "Content-Type: application/json" \
                    -d '{"message": "Cześć! To jest test integracji."}' || echo "failed")
                
                if [ "$message_response" != "failed" ]; then
                    print_success "Chat integration test completed successfully"
                    echo "Response preview: $(echo "$message_response" | jq -r '.data.content' 2>/dev/null | head -c 100)..."
                else
                    print_warning "Chat message test failed"
                fi
            fi
        fi
    else
        print_warning "Orchestrator API is not running. Start it to test full integration."
    fi
}

# Monitor resource usage
monitor_resources() {
    print_status "Monitoring resource usage..."
    
    # Check Docker container status
    echo "Docker container status:"
    docker ps --filter "name=es" --filter "name=kibana" --filter "name=embedding-service" --filter "name=llm-service" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    
    echo
    echo "Memory usage:"
    docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}" es kibana embedding-service llm-service 2>/dev/null || echo "Some containers may not be running"
}

# Main execution
main() {
    echo -e "${BLUE}"
    echo "================================================="
    echo "      RAG Suite LLM Integration Test"
    echo "================================================="
    echo -e "${NC}"
    
    test_services
    echo
    test_orchestrator_api
    echo
    monitor_resources
    
    echo
    echo -e "${GREEN}"
    echo "================================================="
    echo "              Test Complete!"
    echo "================================================="
    echo -e "${NC}"
    echo
    echo "Next steps if issues found:"
    echo "  1. Check Docker logs: docker-compose logs [service-name]"
    echo "  2. Verify environment configuration in .env file"
    echo "  3. Ensure sufficient system resources"
    echo "  4. Check service health endpoints individually"
}

# Run the main function
main "$@"
