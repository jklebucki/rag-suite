# Skrypt do reinstalacji serwisu embedding z nową konfiguracją
# Autor: RAG-Suite
# Data: 2025-09-03

Write-Host "=== RAG-Suite: Reinstalacja serwisu embedding ===" -ForegroundColor Green
Write-Host "Data: $(Get-Date)" -ForegroundColor Gray
Write-Host ""

try {
    # Przejdź do katalogu ze skryptem
    Set-Location $PSScriptRoot

    Write-Host "1. Zatrzymywanie serwisu embedding..." -ForegroundColor Yellow
    docker-compose -f compose.yml stop embedding-service

    Write-Host "2. Usuwanie kontenera embedding..." -ForegroundColor Yellow
    docker-compose -f compose.yml rm -f embedding-service

    Write-Host "3. Pobieranie najnowszego obrazu..." -ForegroundColor Yellow
    docker-compose -f compose.yml pull embedding-service

    Write-Host "4. Uruchamianie serwisu embedding z nową konfiguracją..." -ForegroundColor Yellow
    docker-compose -f compose.yml up -d embedding-service

    Write-Host "5. Sprawdzanie statusu serwisu..." -ForegroundColor Yellow
    Start-Sleep 10

    # Sprawdź czy serwis jest dostępny
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:8580/health" -Method GET -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ Serwis embedding działa poprawnie" -ForegroundColor Green
        } else {
            throw "Serwis nie odpowiada poprawnie"
        }
    } catch {
        Write-Host "❌ Serwis embedding nie odpowiada" -ForegroundColor Red
        Write-Host "Sprawdź logi: docker logs embedding-service-srv" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "6. Sprawdzanie nowej konfiguracji..." -ForegroundColor Yellow
    Write-Host "Aktualna konfiguracja serwisu:" -ForegroundColor Cyan
    try {
        $config = Invoke-RestMethod -Uri "http://localhost:8580/info" -Method GET
        $config | ConvertTo-Json -Depth 3 | Write-Host
    } catch {
        Write-Host "Nie można pobrać konfiguracji" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "✅ Reinstalacja zakończona pomyślnie!" -ForegroundColor Green
    Write-Host "📋 Nowe parametry:" -ForegroundColor Cyan
    Write-Host "   - max_input_length: 512 tokenów" -ForegroundColor White
    Write-Host "   - auto_truncate: włączone" -ForegroundColor White
    Write-Host ""
    Write-Host "Możesz teraz uruchomić RAG.Collector z większymi chunkami." -ForegroundColor Green

} catch {
    Write-Host "❌ Błąd podczas reinstalacji: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
