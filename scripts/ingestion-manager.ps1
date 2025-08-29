# RAG Ingestion Management Script - PowerShell Version
# Cross-platform compatible version for Windows

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("build", "run", "test", "add-documents", "check-es", "clean", "help")]
    [string]$Command
)

# Get script directory and project root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$IngestionProject = Join-Path $ProjectRoot "src\RAG.Ingestion.Worker"
$DocumentsPath = Join-Path $ProjectRoot "data\documents"

# Colors for output (if supported)
$SupportsColor = $Host.UI.RawUI.ForegroundColor -ne $null

function Write-ColoredOutput {
    param([string]$Message, [string]$Color = "White")
    
    if ($SupportsColor) {
        switch ($Color) {
            "Red" { Write-Host $Message -ForegroundColor Red }
            "Green" { Write-Host $Message -ForegroundColor Green }
            "Yellow" { Write-Host $Message -ForegroundColor Yellow }
            "Blue" { Write-Host $Message -ForegroundColor Blue }
            default { Write-Host $Message }
        }
    } else {
        Write-Host $Message
    }
}

function Show-Help {
    Write-Host "Usage: .\ingestion-manager.ps1 -Command <command>"
    Write-Host ""
    Write-Host "Commands:"
    Write-Host "  build         - Build the ingestion worker"
    Write-Host "  run           - Run the ingestion worker"
    Write-Host "  test          - Test Elasticsearch connection"
    Write-Host "  add-documents - Add sample documents to process"
    Write-Host "  check-es      - Check Elasticsearch status"
    Write-Host "  clean         - Clean up build artifacts"
    Write-Host "  help          - Show this help message"
}

function Build-Project {
    Write-ColoredOutput "🔨 Building RAG.Ingestion.Worker..." "Yellow"
    
    Push-Location $IngestionProject
    try {
        dotnet build
        if ($LASTEXITCODE -eq 0) {
            Write-ColoredOutput "✅ Build successful" "Green"
        } else {
            Write-ColoredOutput "❌ Build failed" "Red"
            exit 1
        }
    } finally {
        Pop-Location
    }
}

function Run-Worker {
    Write-ColoredOutput "🚀 Starting RAG Ingestion Worker..." "Yellow"
    
    Push-Location $IngestionProject
    try {
        dotnet run
    } finally {
        Pop-Location
    }
}

function Test-Elasticsearch {
    Write-ColoredOutput "🔍 Testing Elasticsearch connection..." "Yellow"
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:9200" -Method Get -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-ColoredOutput "✅ Elasticsearch is running" "Green"
            
            # Get cluster info
            Write-ColoredOutput "Cluster Info:" "Yellow"
            $clusterInfo = Invoke-RestMethod -Uri "http://localhost:9200" -Method Get
            Write-Host "Name: $($clusterInfo.name)"
            Write-Host "Cluster: $($clusterInfo.cluster_name)"
            Write-Host "Version: $($clusterInfo.version.number)"
            
            # Check indices
            Write-ColoredOutput "Indices:" "Yellow"
            $indices = Invoke-WebRequest -Uri "http://localhost:9200/_cat/indices?v" -Method Get -UseBasicParsing
            Write-Host $indices.Content
        }
    } catch {
        Write-ColoredOutput "❌ Elasticsearch is not accessible" "Red"
        Write-Host "Make sure Elasticsearch is running on http://localhost:9200"
        Write-Host "Error: $($_.Exception.Message)"
    }
}

function Add-SampleDocuments {
    Write-ColoredOutput "📄 Adding sample documents..." "Yellow"
    
    # Ensure documents directory exists
    if (!(Test-Path $DocumentsPath)) {
        New-Item -ItemType Directory -Path $DocumentsPath -Force | Out-Null
    }
    
    # Create sample company policies document
    $policiesContent = @"
# Polityki Firmowe - Przykład

## Polityka Bezpieczeństwa IT

### 1. Hasła
- Minimalna długość: 12 znaków
- Wymagane: wielkie i małe litery, cyfry, znaki specjalne
- Zmiana co 90 dni
- Zakaz używania poprzednich 12 haseł

### 2. Dostęp do Systemów
- Uwierzytelnianie dwuskładnikowe obowiązkowe
- Dostęp na zasadzie najmniejszych uprawnień
- Regularne audyty dostępu

### 3. Bezpieczeństwo Danych
- Szyfrowanie danych w ruchu i spoczynku
- Klasyfikacja danych (publiczne, wewnętrzne, poufne, tajne)
- Backup codziennie, test odtwarzania miesięcznie

## Polityka Pracy Zdalnej

### Wymagania Techniczne
- VPN do połączenia z siecią firmową
- Antywirus aktualny na urządzeniu
- Automatyczne blokowanie ekranu po 10 min

### Bezpieczeństwo
- Zakaz pracy w miejscach publicznych z danymi poufnymi
- Używanie słuchawek podczas rozmów biznesowych
- Zabezpieczenie fizyczne urządzenia
"@
    
    $policiesPath = Join-Path $DocumentsPath "company-policies.txt"
    Set-Content -Path $policiesPath -Value $policiesContent -Encoding UTF8
    
    # Create sample API documentation
    $apiDocContent = @"
# API Documentation - RAG Orchestrator

## Overview
RAG Orchestrator API provides endpoints for chat interactions and document search.

## Authentication
All requests require API key in header:
```
Authorization: Bearer YOUR_API_KEY
```

## Endpoints

### Chat API

#### POST /api/chat/sessions
Create new chat session
```json
{
  "userId": "string",
  "title": "string"
}
```

#### POST /api/chat/sessions/{sessionId}/messages
Send message to chat
```json
{
  "message": "string",
  "useRag": true
}
```

#### GET /api/chat/sessions/{sessionId}
Get chat session details

### Search API

#### GET /api/search
Search documents
```
GET /api/search?query=elasticsearch&limit=10
```

#### POST /api/search
Advanced search
```json
{
  "query": "string",
  "filters": {},
  "limit": 10,
  "useVector": true
}
```

## Response Format
```json
{
  "success": true,
  "data": {},
  "error": null,
  "timestamp": "2024-08-24T10:00:00Z"
}
```

## Error Codes
- 400: Bad Request
- 401: Unauthorized
- 404: Not Found
- 500: Internal Server Error
"@
    
    $apiDocPath = Join-Path $DocumentsPath "api-documentation.md"
    Set-Content -Path $apiDocPath -Value $apiDocContent -Encoding UTF8
    
    Write-ColoredOutput "✅ Sample documents created in $DocumentsPath" "Green"
    Write-Host "Documents:"
    Get-ChildItem $DocumentsPath | Format-Table Name, Length, LastWriteTime
}

function Check-ElasticsearchStatus {
    Write-ColoredOutput "📊 Elasticsearch Status Check..." "Yellow"
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:9200" -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-ColoredOutput "✅ Elasticsearch is running" "Green"
            
            # Check cluster health
            Write-ColoredOutput "Cluster Health:" "Yellow"
            $health = Invoke-RestMethod -Uri "http://localhost:9200/_cluster/health?pretty"
            Write-Host ($health | ConvertTo-Json -Depth 3)
            
            # Check RAG indices
            Write-ColoredOutput "RAG Indices:" "Yellow"
            try {
                $indices = Invoke-WebRequest -Uri "http://localhost:9200/_cat/indices/rag_*?v" -UseBasicParsing
                Write-Host $indices.Content
            } catch {
                Write-Host "No RAG indices found"
            }
            
            # Check document count
            Write-ColoredOutput "Document Count:" "Yellow"
            try {
                $count = Invoke-RestMethod -Uri "http://localhost:9200/rag_documents/_count"
                Write-Host "Documents: $($count.count)"
            } catch {
                Write-Host "Index not found or error accessing it"
            }
        }
    } catch {
        Write-ColoredOutput "❌ Elasticsearch is not running" "Red"
        Write-Host "Error: $($_.Exception.Message)"
    }
}

function Clean-Project {
    Write-ColoredOutput "🧹 Cleaning build artifacts..." "Yellow"
    
    Push-Location $IngestionProject
    try {
        dotnet clean
        Write-ColoredOutput "✅ Clean completed" "Green"
    } finally {
        Pop-Location
    }
}

# Main script logic
Write-ColoredOutput "🔍 RAG Document Ingestion Management" "Blue"
Write-Host "====================================="

switch ($Command) {
    "build" { Build-Project }
    "run" { 
        Build-Project
        Run-Worker 
    }
    "test" { Test-Elasticsearch }
    "add-documents" { Add-SampleDocuments }
    "check-es" { Check-ElasticsearchStatus }
    "clean" { Clean-Project }
    "help" { Show-Help }
    default { 
        Write-ColoredOutput "Unknown command: $Command" "Red"
        Show-Help
        exit 1 
    }
}
