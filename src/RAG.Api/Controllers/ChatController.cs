using MediatR;
using Microsoft.AspNetCore.Mvc;
using RAG.Application.Commands.SendMessage;
using RAG.Application.Queries.GetSessions;
using RAG.Application.Services;

namespace RAG.Api.Controllers;

/// <summary>
/// Modern chat controller using CQRS with MediatR and Semantic Kernel
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IMediator mediator, ILogger<ChatController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Send a message in a chat session with RAG
    /// </summary>
    [HttpPost("sessions/{sessionId}/messages")]
    public async Task<ActionResult<SendMessageResult>> SendMessage(
        string sessionId, 
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new SendMessageCommand
            {
                SessionId = sessionId,
                Message = request.Message,
                UseRAG = request.UseRAG,
                UseBusinessProcessPlugin = request.UseBusinessProcessPlugin,
                UseOraclePlugin = request.UseOraclePlugin
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to session {SessionId}", sessionId);
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all chat sessions
    /// </summary>
    [HttpGet("sessions")]
    public async Task<ActionResult<IEnumerable<ChatSessionDto>>> GetSessions(CancellationToken cancellationToken)
    {
        var query = new GetSessionsQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new chat session
    /// </summary>
    [HttpPost("sessions")]
    public async Task<ActionResult<ChatSessionDto>> CreateSession(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateSessionCommand { Title = request.Title };
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetSession), new { sessionId = result.Id }, result);
    }

    /// <summary>
    /// Get a specific chat session
    /// </summary>
    [HttpGet("sessions/{sessionId}")]
    public async Task<ActionResult<ChatSessionDto>> GetSession(string sessionId, CancellationToken cancellationToken)
    {
        var query = new GetSessionQuery { SessionId = sessionId };
        var result = await _mediator.Send(query, cancellationToken);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }

    /// <summary>
    /// Delete a chat session
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    public async Task<ActionResult> DeleteSession(string sessionId, CancellationToken cancellationToken)
    {
        var command = new DeleteSessionCommand { SessionId = sessionId };
        var success = await _mediator.Send(command, cancellationToken);
        
        if (!success)
            return NotFound();
            
        return NoContent();
    }

    /// <summary>
    /// Get health status including Semantic Kernel plugins
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<ChatHealthStatus>> GetHealth(
        [FromServices] ISemanticKernelService semanticKernelService,
        CancellationToken cancellationToken)
    {
        try
        {
            var plugins = await semanticKernelService.GetAvailablePluginsAsync();
            
            return Ok(new ChatHealthStatus
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                AvailablePlugins = plugins.Select(p => p.Name).ToList(),
                SemanticKernelVersion = "1.24.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return Ok(new ChatHealthStatus
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            });
        }
    }
}

// DTOs
public record SendMessageRequest
{
    public string Message { get; init; } = string.Empty;
    public bool UseRAG { get; init; } = true;
    public bool UseBusinessProcessPlugin { get; init; } = false;
    public bool UseOraclePlugin { get; init; } = false;
}

public record CreateSessionRequest
{
    public string? Title { get; init; }
}

public record ChatHealthStatus
{
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public List<string> AvailablePlugins { get; init; } = new();
    public string? SemanticKernelVersion { get; init; }
    public string? Error { get; init; }
}

// Temporary command/query definitions - will be moved to Application layer
public record CreateSessionCommand : IRequest<ChatSessionDto>
{
    public string? Title { get; init; }
}

public record DeleteSessionCommand : IRequest<bool>
{
    public string SessionId { get; init; } = string.Empty;
}

public record GetSessionQuery : IRequest<ChatSessionDto?>
{
    public string SessionId { get; init; } = string.Empty;
}

public record ChatSessionDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<ChatMessageDto> Messages { get; init; } = new();
}

public record ChatMessageDto
{
    public string Id { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public List<SourceReference>? Sources { get; init; }
}
