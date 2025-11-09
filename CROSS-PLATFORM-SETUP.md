# Cross-Platform Setup Guide

## Overview

This guide helps you set up RAG Suite on any operating system - Windows, macOS, or Linux.

## Prerequisites

### Required Tools

- **Docker & Docker Compose**: For running Elasticsearch, Ollama, and other services
- **.NET 8 SDK**: For building and running the application
- **PowerShell** (Optional): For using the cross-platform setup script

### Platform-Specific Installation

#### Windows
```powershell
# Install via winget
winget install Docker.DockerDesktop
winget install Microsoft.DotNet.SDK.8

# Or download from:
# - Docker: https://docker.com/products/docker-desktop
# - .NET: https://dotnet.microsoft.com/download
```

#### macOS
```bash
# Install via Homebrew
brew install docker
brew install dotnet

# Or download Docker Desktop from:
# https://docker.com/products/docker-desktop
```

#### Linux (Ubuntu/Debian)
```bash
# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Install .NET 8
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0
```

## Quick Start

### Option 1: Cross-Platform PowerShell Script (Recommended)

```powershell
# Clone the repository
git clone https://github.com/jklebucki/rag-suite.git
cd rag-suite

# Run setup (works on Windows, macOS, Linux with PowerShell)
./scripts/setup-cross-platform.ps1 -Command setup
```

### Option 2: Platform-Specific Scripts

#### Windows
```powershell
# PowerShell script for Windows
.\scripts\ingestion-manager.ps1 -Command run
```

#### macOS/Linux
```bash
# Bash script for Unix-like systems
./scripts/ingestion-manager.sh run
```

### Option 3: Manual Setup

#### 1. Start Infrastructure Services
```bash
cd deploy
docker-compose -f docker-compose.cross-platform.yml up -d
```

#### 2. Wait for Services
```bash
# Check if services are ready
curl http://localhost:9200  # Elasticsearch
curl http://localhost:11434/api/tags  # Ollama
curl http://192.168.21.14:8580/health  # Embedding Service
```

#### 3. Create Data Directory
```bash
# Windows
mkdir data\documents

# macOS/Linux
mkdir -p data/documents
```

#### 4. Run Collector Service
```bash
cd src/RAG.Collector
dotnet run
```

#### 5. Run API Server
```bash
cd src/RAG.Orchestrator.Api
dotnet run
```

#### 6. Run Web UI
```bash
cd src/RAG.Web.UI
npm install
npm run dev
```

## Service URLs

Once setup is complete, services will be available at:

- **Web UI**: http://localhost:3000
- **API**: http://localhost:7107
- **Elasticsearch**: http://localhost:9200 (elastic/elastic)
- **Kibana**: http://localhost:5601
- **Ollama (LLM)**: http://localhost:11434
- **Embedding Service**: http://192.168.21.14:8580

## Configuration

### Document Path

The system uses cross-platform path resolution. Documents should be placed in:

```
<project-root>/data/documents/
```

The path is automatically resolved based on your operating system.

### Environment Variables

You can override default settings using environment variables:

```bash
# Override documents path
export RAG_DOCUMENTS_PATH="/custom/path/to/documents"

# Override Elasticsearch URL
export RAG_ELASTICSEARCH_URL="http://localhost:9200"
```

## Troubleshooting

### Common Issues

#### Docker Permission Issues (Linux)
```bash
# Add user to docker group
sudo usermod -aG docker $USER
# Log out and back in
```

#### Port Conflicts
If ports are already in use, stop conflicting services or modify `docker-compose.cross-platform.yml`.

#### Path Issues
The system automatically handles path differences between Windows (`\`) and Unix (`/`). If you encounter path issues, check that:
- The `data/documents` directory exists
- You have read/write permissions

#### Service Health Check
Use the status command to check service health:

```powershell
./scripts/setup-cross-platform.ps1 -Command status
```

### OS-Specific Notes

#### Windows
- Uses PowerShell scripts (`.ps1`)
- Paths use backslashes (`\`) internally but are automatically converted
- Docker Desktop must be running

#### macOS
- Supports both Bash and PowerShell scripts
- Apple Silicon (M1/M2) uses ARM64 Docker images
- Intel Macs use x86_64 images

#### Linux
- Primarily uses Bash scripts
- PowerShell Core can be installed for cross-platform scripts
- Supports both x86_64 and ARM64 architectures

## Development

### Building from Source

```bash
# Build all projects
dotnet build

# Build specific project
dotnet build src/RAG.Orchestrator.Api

# Run tests
dotnet test
```

### Adding Documents

1. Place documents in `data/documents/`
2. Supported formats: PDF, Word, Excel, Text, Markdown
3. The ingestion worker will automatically process new files

### API Development

The API follows RESTful conventions:
- Chat: `/api/chat/`
- Search: `/api/search/`
- Plugins: `/api/plugins/`

### Frontend Development

The Web UI is built with React + TypeScript + Vite:

```bash
cd src/RAG.Web.UI
npm run dev      # Development server
npm run build    # Production build
npm run preview  # Preview production build
```

## License

[Your License Here]

## Contributing

[Contributing guidelines here]
