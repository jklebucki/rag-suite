# Cross-Platform RAG Suite Setup Script - PowerShell Version
# Works on Windows, Linux (with PowerShell Core), and macOS

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("setup", "start", "stop", "status", "clean", "help")]
    [string]$Command = "help"
)

# Cross-platform path handling
$IsWindowsOS = $IsWindows -or ($PSVersionTable.PSVersion.Major -lt 6)
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

function Write-ColoredOutput {
    param([string]$Message, [string]$Color = "White")
    
    $ColorMap = @{
        "Red" = [ConsoleColor]::Red
        "Green" = [ConsoleColor]::Green
        "Yellow" = [ConsoleColor]::Yellow
        "Blue" = [ConsoleColor]::Blue
        "Cyan" = [ConsoleColor]::Cyan
        "Magenta" = [ConsoleColor]::Magenta
    }
    
    if ($ColorMap.ContainsKey($Color)) {
        Write-Host $Message -ForegroundColor $ColorMap[$Color]
    } else {
        Write-Host $Message
    }
}

function Get-OSInfo {
    if ($IsWindowsOS) {
        return @{ Name = "Windows"; Platform = "windows" }
    } elseif ($IsLinux) {
        return @{ Name = "Linux"; Platform = "linux" }
    } elseif ($IsMacOS) {
        return @{ Name = "macOS"; Platform = "macos" }
    } else {
        return @{ Name = "Unknown"; Platform = "unknown" }
    }
}

function Test-RequiredTools {
    Write-ColoredOutput "🔍 Checking required tools..." "Blue"
    
    $tools = @(
        @{ Name = "Docker"; Command = "docker --version" },
        @{ Name = "Docker Compose"; Command = "docker-compose --version" },
        @{ Name = ".NET 8 SDK"; Command = "dotnet --version" }
    )
    
    $missing = @()
    
    foreach ($tool in $tools) {
        try {
            $result = Invoke-Expression $tool.Command 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-ColoredOutput "✅ $($tool.Name) is installed" "Green"
            } else {
                $missing += $tool.Name
            }
        } catch {
            $missing += $tool.Name
        }
    }
    
    if ($missing.Count -gt 0) {
        Write-ColoredOutput "❌ Missing required tools: $($missing -join ', ')" "Red"
        return $false
    }
    
    return $true
}

function Setup-Environment {
    $osInfo = Get-OSInfo
    Write-ColoredOutput "🚀 Setting up RAG Suite on $($osInfo.Name)..." "Blue"
    
    if (-not (Test-RequiredTools)) {
        Write-ColoredOutput "Please install missing tools and try again." "Red"
        return $false
    }
    
    # Create data directories
    $dataDir = Join-Path $ProjectRoot "data"
    $documentsDir = Join-Path $dataDir "documents"
    
    if (-not (Test-Path $dataDir)) {
        New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
        Write-ColoredOutput "✅ Created data directory" "Green"
    }
    
    if (-not (Test-Path $documentsDir)) {
        New-Item -ItemType Directory -Path $documentsDir -Force | Out-Null
        Write-ColoredOutput "✅ Created documents directory" "Green"
    }
    
    # Use cross-platform docker-compose file
    $composeFile = Join-Path $ProjectRoot "deploy\docker-compose.cross-platform.yml"
    
    Write-ColoredOutput "🐳 Starting Docker services..." "Yellow"
    
    Push-Location (Join-Path $ProjectRoot "deploy")
    try {
        $dockerCmd = "docker-compose -f docker-compose.cross-platform.yml up -d"
        Invoke-Expression $dockerCmd
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColoredOutput "✅ Docker services started successfully" "Green"
            
            Write-ColoredOutput "⏳ Waiting for services to be ready..." "Yellow"
            Start-Sleep -Seconds 30
            
            Test-ServicesHealth
            
        } else {
            Write-ColoredOutput "❌ Failed to start Docker services" "Red"
            return $false
        }
    } finally {
        Pop-Location
    }
    
    return $true
}

function Test-ServicesHealth {
    Write-ColoredOutput "🏥 Checking services health..." "Blue"
    
    $services = @(
        @{ Name = "Elasticsearch"; Url = "http://localhost:9200"; Auth = "elastic:elastic" },
        @{ Name = "Kibana"; Url = "http://localhost:5601" },
        @{ Name = "Ollama"; Url = "http://localhost:11434/api/tags" },
        @{ Name = "Embedding Service"; Url = "http://localhost:8580/health" }
    )
    
    foreach ($service in $services) {
        try {
            if ($service.Auth) {
                $credentials = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($service.Auth))
                $headers = @{ Authorization = "Basic $credentials" }
                $response = Invoke-WebRequest -Uri $service.Url -Headers $headers -TimeoutSec 10 -UseBasicParsing
            } else {
                $response = Invoke-WebRequest -Uri $service.Url -TimeoutSec 10 -UseBasicParsing
            }
            
            if ($response.StatusCode -eq 200) {
                Write-ColoredOutput "✅ $($service.Name) is healthy" "Green"
            } else {
                Write-ColoredOutput "⚠️ $($service.Name) returned status: $($response.StatusCode)" "Yellow"
            }
        } catch {
            Write-ColoredOutput "❌ $($service.Name) is not responding" "Red"
        }
    }
}

function Start-Services {
    Write-ColoredOutput "🚀 Starting RAG Suite services..." "Blue"
    
    Push-Location (Join-Path $ProjectRoot "deploy")
    try {
        docker-compose -f docker-compose.cross-platform.yml up -d
        if ($LASTEXITCODE -eq 0) {
            Write-ColoredOutput "✅ Services started" "Green"
            Start-Sleep -Seconds 10
            Test-ServicesHealth
        } else {
            Write-ColoredOutput "❌ Failed to start services" "Red"
        }
    } finally {
        Pop-Location
    }
}

function Stop-Services {
    Write-ColoredOutput "🛑 Stopping RAG Suite services..." "Blue"
    
    Push-Location (Join-Path $ProjectRoot "deploy")
    try {
        docker-compose -f docker-compose.cross-platform.yml down
        if ($LASTEXITCODE -eq 0) {
            Write-ColoredOutput "✅ Services stopped" "Green"
        } else {
            Write-ColoredOutput "❌ Failed to stop services" "Red"
        }
    } finally {
        Pop-Location
    }
}

function Show-Status {
    Write-ColoredOutput "📊 RAG Suite Status" "Blue"
    Write-Host "==================="
    
    $osInfo = Get-OSInfo
    Write-Host "OS: $($osInfo.Name)"
    Write-Host "Project Root: $ProjectRoot"
    
    Write-Host ""
    Test-ServicesHealth
    
    Write-Host ""
    Write-ColoredOutput "Docker Containers:" "Yellow"
    docker ps --filter "name=es" --filter "name=kibana" --filter "name=ollama" --filter "name=embedding" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
}

function Clean-Environment {
    Write-ColoredOutput "🧹 Cleaning RAG Suite environment..." "Blue"
    
    Push-Location (Join-Path $ProjectRoot "deploy")
    try {
        docker-compose -f docker-compose.cross-platform.yml down -v --remove-orphans
        Write-ColoredOutput "✅ Docker environment cleaned" "Green"
    } finally {
        Pop-Location
    }
    
    # Clean .NET build artifacts
    $srcDir = Join-Path $ProjectRoot "src"
    if (Test-Path $srcDir) {
        Push-Location $srcDir
        try {
            dotnet clean -v minimal
            Write-ColoredOutput "✅ .NET build artifacts cleaned" "Green"
        } finally {
            Pop-Location
        }
    }
}

function Show-Help {
    $osInfo = Get-OSInfo
    
    Write-Host ""
    Write-ColoredOutput "RAG Suite Cross-Platform Setup Script" "Blue"
    Write-Host "======================================"
    Write-Host "Running on: $($osInfo.Name)"
    Write-Host ""
    Write-Host "Usage: ./setup-cross-platform.ps1 -Command <command>"
    Write-Host ""
    Write-Host "Commands:"
    Write-Host "  setup   - Initial setup and start all services"
    Write-Host "  start   - Start all services"
    Write-Host "  stop    - Stop all services"
    Write-Host "  status  - Show services status"
    Write-Host "  clean   - Clean environment and remove containers"
    Write-Host "  help    - Show this help message"
    Write-Host ""
    Write-Host "Services will be available at:"
    Write-Host "  • Elasticsearch: http://localhost:9200 (elastic/elastic)"
    Write-Host "  • Kibana: http://localhost:5601"
    Write-Host "  • Ollama (LLM): http://localhost:11434"
    Write-Host "  • Embedding Service: http://localhost:8580"
    Write-Host ""
}

# Main execution
switch ($Command) {
    "setup" { 
        if (Setup-Environment) {
            Write-Host ""
            Write-ColoredOutput "🎉 RAG Suite setup completed successfully!" "Green"
            Write-Host ""
            Show-Status
        }
    }
    "start" { Start-Services }
    "stop" { Stop-Services }
    "status" { Show-Status }
    "clean" { Clean-Environment }
    "help" { Show-Help }
    default { 
        Write-ColoredOutput "Unknown command: $Command" "Red"
        Show-Help
        exit 1 
    }
}
