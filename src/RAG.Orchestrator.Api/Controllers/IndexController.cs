using Microsoft.AspNetCore.Mvc;
using RAG.Orchestrator.Api.Features.Search;

namespace RAG.Orchestrator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IndexController : ControllerBase
{
    private readonly IIndexManagementService _indexManagement;
    private readonly ILogger<IndexController> _logger;

    public IndexController(IIndexManagementService indexManagement, ILogger<IndexController> logger)
    {
        _indexManagement = indexManagement;
        _logger = logger;
    }

    [HttpGet("available")]
    public async Task<ActionResult<string[]>> GetAvailableIndices(CancellationToken cancellationToken = default)
    {
        try
        {
            var indices = await _indexManagement.GetAvailableIndicesAsync(cancellationToken);
            return Ok(indices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available indices");
            return StatusCode(500, new { Error = "Failed to retrieve available indices" });
        }
    }

    [HttpGet("{indexName}/exists")]
    public async Task<ActionResult<bool>> CheckIndexExists(string indexName, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _indexManagement.IndexExistsAsync(indexName, cancellationToken);
            return Ok(new { IndexName = indexName, Exists = exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if index {IndexName} exists", indexName);
            return StatusCode(500, new { Error = $"Failed to check if index {indexName} exists" });
        }
    }

    [HttpPost("{indexName}/create")]
    public async Task<ActionResult> CreateIndex(string indexName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (await _indexManagement.IndexExistsAsync(indexName, cancellationToken))
            {
                return Conflict(new { Error = $"Index {indexName} already exists" });
            }

            await _indexManagement.CreateIndexAsync(indexName, cancellationToken);
            return Ok(new { Message = $"Index {indexName} created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating index {IndexName}", indexName);
            return StatusCode(500, new { Error = $"Failed to create index {indexName}" });
        }
    }

    [HttpPost("{indexName}/ensure")]
    public async Task<ActionResult> EnsureIndexExists(string indexName, CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await _indexManagement.EnsureIndexExistsAsync(indexName, cancellationToken);
            return Ok(new { 
                IndexName = indexName, 
                Action = created ? "Index verified/created" : "Index check failed",
                Success = created 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring index {IndexName} exists", indexName);
            return StatusCode(500, new { Error = $"Failed to ensure index {indexName} exists" });
        }
    }
}
