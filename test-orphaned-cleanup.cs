using System.Text.Json;

namespace RAG.Collector.TestOrphanedCleanup;

/// <summary>
/// Simple console application to test the orphaned document cleanup functionality
/// </summary>
class Program
{
    private static readonly HttpClient _httpClient = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("RAG Collector - Orphaned Document Cleanup Test");
        Console.WriteLine("==============================================");

        try
        {
            // Test connection to Elasticsearch
            var esUrl = "http://192.168.21.13:9200";
            var indexName = "rag-chunks";
            
            Console.WriteLine($"Testing Elasticsearch connection to: {esUrl}");
            
            var authHeader = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("elastic:elastic"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

            // Get all unique source files
            var uniqueFiles = await GetUniqueSourceFilesAsync(esUrl, indexName);
            Console.WriteLine($"Found {uniqueFiles.Count} unique files in Elasticsearch:");

            var orphanedFiles = new List<(string filePath, int chunkCount)>();
            var existingFiles = new List<(string filePath, int chunkCount)>();

            foreach (var file in uniqueFiles)
            {
                var exists = File.Exists(file.Key);
                Console.WriteLine($"  {(exists ? "✓" : "✗")} {file.Key} ({file.Value} chunks)");
                
                if (exists)
                {
                    existingFiles.Add((file.Key, file.Value));
                }
                else
                {
                    orphanedFiles.Add((file.Key, file.Value));
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Summary:");
            Console.WriteLine($"  Total files in index: {uniqueFiles.Count}");
            Console.WriteLine($"  Existing files: {existingFiles.Count}");
            Console.WriteLine($"  Orphaned files: {orphanedFiles.Count}");
            Console.WriteLine($"  Total orphaned chunks: {orphanedFiles.Sum(f => f.chunkCount)}");

            if (orphanedFiles.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Orphaned files (would be cleaned up):");
                foreach (var orphaned in orphanedFiles)
                {
                    Console.WriteLine($"  - {orphaned.filePath} ({orphaned.chunkCount} chunks)");
                }

                Console.WriteLine();
                Console.Write("Do you want to delete these orphaned documents? (y/N): ");
                var response = Console.ReadLine();
                
                if (response?.ToLower() == "y" || response?.ToLower() == "yes")
                {
                    var totalDeleted = 0;
                    foreach (var orphaned in orphanedFiles)
                    {
                        var deleted = await DeleteDocumentsBySourceFileAsync(esUrl, indexName, orphaned.filePath);
                        totalDeleted += deleted;
                        Console.WriteLine($"Deleted {deleted} documents for {orphaned.filePath}");
                    }
                    
                    Console.WriteLine($"Total documents deleted: {totalDeleted}");
                }
                else
                {
                    Console.WriteLine("No documents were deleted.");
                }
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("No orphaned documents found. All files in the index exist on disk.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static async Task<Dictionary<string, int>> GetUniqueSourceFilesAsync(string esUrl, string indexName)
    {
        var query = new
        {
            size = 0,
            aggs = new
            {
                unique_files = new
                {
                    terms = new
                    {
                        field = "sourceFile.keyword",
                        size = 10000
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(query);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"{esUrl}/{indexName}/_search", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Elasticsearch request failed: {response.StatusCode} - {responseContent}");
        }

        var searchResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        var filePaths = new Dictionary<string, int>();

        if (searchResponse.TryGetProperty("aggregations", out var aggregations) &&
            aggregations.TryGetProperty("unique_files", out var uniqueFiles) &&
            uniqueFiles.TryGetProperty("buckets", out var buckets))
        {
            foreach (var bucket in buckets.EnumerateArray())
            {
                if (bucket.TryGetProperty("key", out var key) &&
                    bucket.TryGetProperty("doc_count", out var docCount))
                {
                    var filePath = key.GetString();
                    var chunkCount = docCount.GetInt32();
                    
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        filePaths[filePath] = chunkCount;
                    }
                }
            }
        }

        return filePaths;
    }

    private static async Task<int> DeleteDocumentsBySourceFileAsync(string esUrl, string indexName, string sourceFile)
    {
        var query = new
        {
            query = new
            {
                term = new
                {
                    sourceFile = new { value = sourceFile }
                }
            }
        };

        var json = JsonSerializer.Serialize(query);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{esUrl}/{indexName}/_delete_by_query", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Delete request failed: {response.StatusCode} - {responseContent}");
        }

        var deleteResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        if (deleteResponse.TryGetProperty("deleted", out var deleted))
        {
            return deleted.GetInt32();
        }

        return 0;
    }
}
