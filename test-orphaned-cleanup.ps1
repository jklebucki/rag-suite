# RAG Collector - Orphaned Document Cleanup Test
# This PowerShell script tests the orphaned document cleanup functionality

param(
    [string]$ElasticsearchUrl = "http://192.168.21.13:9200",
    [string]$IndexName = "rag-chunks",
    [string]$Username = "elastic",
    [string]$Password = "elastic",
    [switch]$DryRun = $true
)

Write-Host "RAG Collector - Orphaned Document Cleanup Test" -ForegroundColor Green
Write-Host "=============================================="

# Create authorization header
$authBytes = [System.Text.Encoding]::ASCII.GetBytes("${Username}:${Password}")
$authHeader = [System.Convert]::ToBase64String($authBytes)

$headers = @{
    "Authorization" = "Basic $authHeader"
    "Content-Type" = "application/json"
}

try {
    # Test Elasticsearch connection
    Write-Host "Testing Elasticsearch connection to: $ElasticsearchUrl"
    $pingResponse = Invoke-RestMethod -Uri "$ElasticsearchUrl" -Headers $headers -Method GET
    Write-Host "✓ Connected to Elasticsearch" -ForegroundColor Green

    # Get all unique source files
    Write-Host "`nRetrieving unique source files from index: $IndexName"
    
    $aggregationQuery = @{
        size = 0
        aggs = @{
            unique_files = @{
                terms = @{
                    field = "sourceFile.keyword"
                    size = 10000
                }
            }
        }
    } | ConvertTo-Json -Depth 10

    $searchResponse = Invoke-RestMethod -Uri "$ElasticsearchUrl/$IndexName/_search" -Headers $headers -Method POST -Body $aggregationQuery
    
    $uniqueFiles = @{}
    if ($searchResponse.aggregations.unique_files.buckets) {
        foreach ($bucket in $searchResponse.aggregations.unique_files.buckets) {
            $uniqueFiles[$bucket.key] = $bucket.doc_count
        }
    }

    Write-Host "Found $($uniqueFiles.Count) unique files in Elasticsearch:"

    $orphanedFiles = @()
    $existingFiles = @()

    foreach ($file in $uniqueFiles.GetEnumerator()) {
        $exists = Test-Path $file.Key
        $status = if ($exists) { "✓" } else { "✗" }
        $color = if ($exists) { "Green" } else { "Red" }
        
        Write-Host "  $status $($file.Key) ($($file.Value) chunks)" -ForegroundColor $color
        
        if ($exists) {
            $existingFiles += @{ FilePath = $file.Key; ChunkCount = $file.Value }
        } else {
            $orphanedFiles += @{ FilePath = $file.Key; ChunkCount = $file.Value }
        }
    }

    $totalOrphanedChunks = ($orphanedFiles | Measure-Object -Property ChunkCount -Sum).Sum

    Write-Host "`nSummary:" -ForegroundColor Yellow
    Write-Host "  Total files in index: $($uniqueFiles.Count)"
    Write-Host "  Existing files: $($existingFiles.Count)" -ForegroundColor Green
    Write-Host "  Orphaned files: $($orphanedFiles.Count)" -ForegroundColor Red
    Write-Host "  Total orphaned chunks: $totalOrphanedChunks" -ForegroundColor Red

    if ($orphanedFiles.Count -gt 0) {
        Write-Host "`nOrphaned files (would be cleaned up):" -ForegroundColor Red
        foreach ($orphaned in $orphanedFiles) {
            Write-Host "  - $($orphaned.FilePath) ($($orphaned.ChunkCount) chunks)" -ForegroundColor Red
        }

        if ($DryRun) {
            Write-Host "`n[DRY RUN MODE] No documents will be deleted." -ForegroundColor Yellow
            Write-Host "To actually delete orphaned documents, run with -DryRun:`$false"
        } else {
            Write-Host "`nDo you want to delete these orphaned documents? (y/N): " -NoNewline
            $response = Read-Host
            
            if ($response -eq "y" -or $response -eq "yes") {
                $totalDeleted = 0
                foreach ($orphaned in $orphanedFiles) {
                    Write-Host "Deleting documents for: $($orphaned.FilePath)"
                    
                    $deleteQuery = @{
                        query = @{
                            term = @{
                                sourceFile = @{
                                    value = $orphaned.FilePath
                                }
                            }
                        }
                    } | ConvertTo-Json -Depth 10

                    $deleteResponse = Invoke-RestMethod -Uri "$ElasticsearchUrl/$IndexName/_delete_by_query" -Headers $headers -Method POST -Body $deleteQuery
                    $deleted = $deleteResponse.deleted
                    $totalDeleted += $deleted
                    Write-Host "  Deleted $deleted documents" -ForegroundColor Green
                }
                
                Write-Host "`nTotal documents deleted: $totalDeleted" -ForegroundColor Green
            } else {
                Write-Host "No documents were deleted."
            }
        }
    } else {
        Write-Host "`nNo orphaned documents found. All files in the index exist on disk." -ForegroundColor Green
    }

} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace: $($_.Exception.StackTrace)" -ForegroundColor Red
}

Write-Host "`nPress any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
