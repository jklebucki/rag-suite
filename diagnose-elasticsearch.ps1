# RAG Suite - Elasticsearch Debug Script (PowerShell)
# Skrypt do diagnozy problemów z rekonstrukcją dokumentów

param(
    [string]$ElasticsearchUrl = "http://localhost:9200",
    [string]$Username = "elastic", 
    [string]$Password = "elastic",
    [string]$IndexName = "rag-chunks"
)

Write-Host "===================================================" -ForegroundColor Green
Write-Host "   RAG Suite - Elasticsearch Diagnostic Tool" -ForegroundColor Green  
Write-Host "===================================================" -ForegroundColor Green

# Funkcja do wykonywania zapytań do Elasticsearch
function Invoke-ESQuery {
    param(
        [string]$Query,
        [string]$Description
    )
    
    Write-Host "[INFO] $Description" -ForegroundColor Yellow
    Write-Host "Query: $Query" -ForegroundColor Gray
    
    $base64Auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$Username`:$Password"))
    $headers = @{
        "Authorization" = "Basic $base64Auth"
        "Content-Type" = "application/json"
    }
    
    try {
        $response = Invoke-RestMethod -Uri "$ElasticsearchUrl/$IndexName/_search" -Method Post -Body $Query -Headers $headers
        
        Write-Host "Total hits: $($response.hits.total.value), took: $($response.took)ms" -ForegroundColor Cyan
        
        $response.hits.hits | Select-Object -First 5 | ForEach-Object {
            $chunkIndex = $_._source.position.chunkIndex
            $contentLength = if ($_._source.content) { $_._source.content.Length } else { 0 }
            $totalChunks = $_._source.position.totalChunks
            Write-Host "Chunk $chunkIndex`: Content length: $contentLength chars, TotalChunks: $totalChunks" -ForegroundColor White
        }
        
        return $response
    }
    catch {
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
    
    Write-Host ""
}

# Test 1: Sprawdź ogólny stan indeksu
Write-Host "[TEST 1] Index Overview" -ForegroundColor Magenta
try {
    $base64Auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$Username`:$Password"))
    $headers = @{ "Authorization" = "Basic $base64Auth" }
    
    $stats = Invoke-RestMethod -Uri "$ElasticsearchUrl/$IndexName/_stats" -Headers $headers
    $docCount = $stats.indices.$IndexName.total.docs.count
    $sizeBytes = $stats.indices.$IndexName.total.store.size_in_bytes
    
    Write-Host "Documents: $docCount, Size: $sizeBytes bytes" -ForegroundColor Cyan
}
catch {
    Write-Host "Error getting index stats: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 2: Sprawdź sample dokumentów
Write-Host "[TEST 2] Sample Documents" -ForegroundColor Magenta
$sampleQuery = @'
{
  "size": 3,
  "_source": ["sourceFile", "content", "position"],
  "query": {"match_all": {}}
}
'@
Invoke-ESQuery -Query $sampleQuery -Description "Getting sample documents"

# Test 3: Sprawdź konkretny dokument z logów
$testFile = "X:\Citronex\IFS Materialy\Instrukcje\Notatka ze spotkania - WinSped - 20240422.docx"
Write-Host "[TEST 3] Specific Document Test" -ForegroundColor Magenta
Write-Host "Testing file: $testFile" -ForegroundColor Gray

# Test 3a: Zapytanie z .keyword
$keywordQuery = @"
{
  "size": 5,
  "_source": ["sourceFile", "content", "position"],
  "query": {
    "term": {
      "sourceFile.keyword": "$testFile"
    }
  },
  "sort": [{"position.chunkIndex": {"order": "asc"}}]
}
"@
Invoke-ESQuery -Query $keywordQuery -Description "Query with .keyword field"

# Test 3b: Zapytanie bez .keyword
$termQuery = @"
{
  "size": 5,
  "_source": ["sourceFile", "content", "position"],
  "query": {
    "term": {
      "sourceFile": "$testFile"
    }
  },
  "sort": [{"position.chunkIndex": {"order": "asc"}}]
}
"@
Invoke-ESQuery -Query $termQuery -Description "Query without .keyword field"

# Test 3c: Zapytanie z match
$matchQuery = @"
{
  "size": 5,
  "_source": ["sourceFile", "content", "position"],
  "query": {
    "match": {
      "sourceFile": "$testFile"
    }
  },
  "sort": [{"position.chunkIndex": {"order": "asc"}}]
}
"@
Invoke-ESQuery -Query $matchQuery -Description "Query with match field"

# Test 4: Sprawdź mapowanie indeksu
Write-Host "[TEST 4] Index Mapping" -ForegroundColor Magenta
try {
    $base64Auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("$Username`:$Password"))
    $headers = @{ "Authorization" = "Basic $base64Auth" }
    
    $mapping = Invoke-RestMethod -Uri "$ElasticsearchUrl/$IndexName/_mapping" -Headers $headers
    $properties = $mapping.$IndexName.mappings.properties
    
    Write-Host "sourceFile mapping: $($properties.sourceFile | ConvertTo-Json -Compress)" -ForegroundColor Cyan
    Write-Host "content mapping: $($properties.content | ConvertTo-Json -Compress)" -ForegroundColor Cyan
    Write-Host "position mapping: $($properties.position | ConvertTo-Json -Compress)" -ForegroundColor Cyan
}
catch {
    Write-Host "Error getting mapping: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 5: Sprawdź unikalne pliki źródłowe
Write-Host "[TEST 5] Unique Source Files (first 10)" -ForegroundColor Magenta
$aggsQuery = @'
{
  "size": 0,
  "aggs": {
    "unique_files": {
      "terms": {
        "field": "sourceFile.keyword",
        "size": 10
      }
    }
  }
}
'@

try {
    $aggsResponse = Invoke-ESQuery -Query $aggsQuery -Description "Getting unique source files"
    if ($aggsResponse) {
        $aggsResponse.aggregations.unique_files.buckets | ForEach-Object {
            Write-Host "$($_.key): $($_.doc_count) chunks" -ForegroundColor White
        }
    }
}
catch {
    Write-Host "Error getting aggregations: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "===================================================" -ForegroundColor Green
Write-Host "           Diagnostic Complete" -ForegroundColor Green
Write-Host "===================================================" -ForegroundColor Green

# Przykład wywołania:
# .\diagnose-elasticsearch.ps1 -ElasticsearchUrl "http://192.168.21.13:9200" -Username "elastic" -Password "yourpassword"
