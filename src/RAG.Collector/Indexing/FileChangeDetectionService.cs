using Microsoft.Extensions.Logging;
using RAG.Collector.Elasticsearch;
using System.Text.Json;

namespace RAG.Collector.Indexing;

/// <summary>
/// Service for checking if files need reindexing based on content changes
/// Uses Elasticsearch to store file metadata and detect changes
/// </summary>
public class FileChangeDetectionService : IFileChangeDetectionService
{
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<FileChangeDetectionService> _logger;
    private const string FILE_METADATA_INDEX = "rag-file-metadata";

    public FileChangeDetectionService(
        IElasticsearchService elasticsearchService,
        ILogger<FileChangeDetectionService> logger)
    {
        _elasticsearchService = elasticsearchService;
        _logger = logger;
    }

    public async Task<bool> ShouldReindexFileAsync(string filePath, string fileHash, DateTime lastModified, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking if file needs reindexing: {FilePath}", filePath);

            // Check if file metadata exists in Elasticsearch
            var existingMetadata = await GetFileMetadataAsync(filePath, cancellationToken);

            if (existingMetadata == null)
            {
                _logger.LogDebug("File not found in metadata index, needs indexing: {FilePath}", filePath);
                return true;
            }

            // Check if content hash changed
            if (existingMetadata.ContentHash != fileHash)
            {
                _logger.LogInformation("File content changed (hash mismatch), needs reindexing: {FilePath}", filePath);
                _logger.LogDebug("Old hash: {OldHash}, New hash: {NewHash}", existingMetadata.ContentHash, fileHash);
                return true;
            }

            // Check if last modified date changed
            if (existingMetadata.LastModified != lastModified)
            {
                _logger.LogInformation("File modified date changed, needs reindexing: {FilePath}", filePath);
                _logger.LogDebug("Old date: {OldDate}, New date: {NewDate}", existingMetadata.LastModified, lastModified);
                return true;
            }

            _logger.LogDebug("File unchanged, skipping reindexing: {FilePath}", filePath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if file needs reindexing: {FilePath}. Defaulting to reindex.", filePath);
            return true; // If in doubt, reindex
        }
    }

    public async Task RecordIndexedFileAsync(string filePath, string fileHash, DateTime lastModified, int chunkCount, CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = new FileMetadataDocument
            {
                Id = GenerateFileId(filePath),
                FilePath = filePath,
                ContentHash = fileHash,
                LastModified = lastModified,
                ChunkCount = chunkCount,
                IndexedAt = DateTime.UtcNow,
                FileExtension = Path.GetExtension(filePath).ToLowerInvariant()
            };

            await IndexFileMetadataAsync(metadata, cancellationToken);
            
            _logger.LogDebug("Recorded indexed file metadata: {FilePath} ({ChunkCount} chunks)", filePath, chunkCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording indexed file metadata: {FilePath}", filePath);
            // Don't throw - this is not critical for the indexing process
        }
    }

    public async Task<FileIndexStats> GetFileIndexStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // This would require implementing aggregation queries in ElasticsearchService
            // For now, return basic stats
            return new FileIndexStats
            {
                TotalIndexedFiles = 0,
                TotalChunks = 0,
                LastIndexedAt = null,
                FilesByExtension = new Dictionary<string, int>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file index stats");
            return new FileIndexStats();
        }
    }

    private async Task<FileMetadataDocument?> GetFileMetadataAsync(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var fileId = GenerateFileId(filePath);
            
            // This would require implementing document get by ID in ElasticsearchService
            // For now, we'll use a search query
            var searchQuery = new
            {
                query = new
                {
                    term = new
                    {
                        filePath = new { value = filePath }
                    }
                },
                size = 1
            };

            // This is a placeholder - would need actual implementation in ElasticsearchService
            // For now, return null to trigger reindexing
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file metadata: {FilePath}", filePath);
            return null;
        }
    }

    private async Task IndexFileMetadataAsync(FileMetadataDocument metadata, CancellationToken cancellationToken)
    {
        try
        {
            // This would require extending ElasticsearchService to support multiple indices
            // For now, we'll log the metadata that would be stored
            _logger.LogDebug("Would store file metadata: {Metadata}", JsonSerializer.Serialize(metadata));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing file metadata: {FilePath}", metadata.FilePath);
        }
    }

    private static string GenerateFileId(string filePath)
    {
        // Generate a consistent ID based on file path
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(filePath))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}

/// <summary>
/// Document model for file metadata storage in Elasticsearch
/// </summary>
public class FileMetadataDocument
{
    public string Id { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public int ChunkCount { get; set; }
    public DateTime IndexedAt { get; set; }
    public string FileExtension { get; set; } = string.Empty;
}
