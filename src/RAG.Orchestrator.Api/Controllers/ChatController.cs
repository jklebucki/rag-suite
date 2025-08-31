using MediatR;
using Microsoft.AspNetCore.Mvc;
using RAG.Application.Commands.CreateSession;
using RAG.Application.Commands.SendMessage;
using RAG.Application.Queries.GetSessions;
using RAG.Application.Queries.GetSessionHistory;
using RAG.Domain.ValueObjects;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;

namespace RAG.Orchestrator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("Chat management with Semantic Kernel integration")]
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
    /// Creates a new chat session
    /// </summary>
    /// <param name="request">Session creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created session details</returns>
    [HttpPost("sessions")]
    [SwaggerOperation(
        Summary = "Create new chat session",
        Description = "Creates a new chat session with optional user context and configuration"
    )]
    [SwaggerResponse(201, "Session created successfully", typeof(CreateSessionResponse))]
    [SwaggerResponse(400, "Invalid request parameters")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<CreateSessionResponse>> CreateSession(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new CreateSessionCommand(
                request.UserId ?? "anonymous",
                request.Context ?? "general"
            );

            var result = await _mediator.Send(command, cancellationToken);
            
            var response = new CreateSessionResponse
            {
                SessionId = result.SessionId,
                UserId = result.UserId,
                Context = result.Context,
                CreatedAt = result.CreatedAt
            };

            return CreatedAtAction(
                nameof(GetSession), 
                new { sessionId = response.SessionId }, 
                response
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chat session for user {UserId}", request.UserId);
            return StatusCode(500, new { Message = "Failed to create session", Error = ex.Message });
        }
    }

    /// <summary>
    /// Sends a message to an existing chat session
    /// </summary>
    /// <param name="sessionId">Chat session identifier</param>
    /// <param name="request">Message content and options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI response with context</returns>
    [HttpPost("sessions/{sessionId}/messages")]
    [SwaggerOperation(
        Summary = "Send message to chat session",
        Description = "Sends a message to the specified chat session and returns AI response with RAG context"
    )]
    [SwaggerResponse(200, "Message processed successfully", typeof(SendMessageResponse))]
    [SwaggerResponse(400, "Invalid request parameters")]
    [SwaggerResponse(404, "Session not found")]
    [SwaggerResponse(429, "Rate limit exceeded")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<SendMessageResponse>> SendMessage(
        [FromRoute] string sessionId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!SessionId.IsValidFormat(sessionId))
            {
                return BadRequest(new { Message = "Invalid session ID format" });
            }

            var command = new SendMessageCommand(
                SessionId.Create(sessionId),
                request.Message,
                request.IncludeContext ?? true,
                request.MaxTokens ?? 1000,
                request.Temperature ?? 0.7f
            );

            var result = await _mediator.Send(command, cancellationToken);
            
            var response = new SendMessageResponse
            {
                SessionId = result.SessionId.Value,
                MessageId = result.MessageId,
                Response = result.Response,
                Context = result.Context?.Select(c => new ContextItem
                {
                    Source = c.Source,
                    Content = c.Content,
                    Score = c.Score
                }).ToList() ?? new List<ContextItem>(),
                TokensUsed = result.TokensUsed,
                ProcessingTimeMs = result.ProcessingTimeMs,
                Timestamp = result.Timestamp
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for session {SessionId}", sessionId);
            return BadRequest(new { Message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Session not found: {SessionId}", sessionId);
            return NotFound(new { Message = "Session not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for session {SessionId}", sessionId);
            return StatusCode(500, new { Message = "Failed to process message", Error = ex.Message });
        }
    }

    /// <summary>
    /// Gets session details
    /// </summary>
    /// <param name="sessionId">Chat session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session details</returns>
    [HttpGet("sessions/{sessionId}")]
    [SwaggerOperation(
        Summary = "Get session details",
        Description = "Retrieves details about a specific chat session"
    )]
    [SwaggerResponse(200, "Session found", typeof(GetSessionResponse))]
    [SwaggerResponse(404, "Session not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<GetSessionResponse>> GetSession(
        [FromRoute] string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!SessionId.IsValidFormat(sessionId))
            {
                return BadRequest(new { Message = "Invalid session ID format" });
            }

            var query = new GetSessionQuery(SessionId.Create(sessionId));
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
            {
                return NotFound(new { Message = "Session not found" });
            }

            var response = new GetSessionResponse
            {
                SessionId = result.SessionId,
                UserId = result.UserId,
                Context = result.Context,
                CreatedAt = result.CreatedAt,
                LastActivityAt = result.LastActivityAt,
                MessageCount = result.MessageCount
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionId}", sessionId);
            return StatusCode(500, new { Message = "Failed to retrieve session", Error = ex.Message });
        }
    }

    /// <summary>
    /// Gets chat history for a session
    /// </summary>
    /// <param name="sessionId">Chat session identifier</param>
    /// <param name="limit">Maximum number of messages to return</param>
    /// <param name="offset">Number of messages to skip</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat history</returns>
    [HttpGet("sessions/{sessionId}/history")]
    [SwaggerOperation(
        Summary = "Get session chat history",
        Description = "Retrieves the chat history for a specific session with pagination"
    )]
    [SwaggerResponse(200, "History retrieved", typeof(GetSessionHistoryResponse))]
    [SwaggerResponse(404, "Session not found")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<GetSessionHistoryResponse>> GetSessionHistory(
        [FromRoute] string sessionId,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!SessionId.IsValidFormat(sessionId))
            {
                return BadRequest(new { Message = "Invalid session ID format" });
            }

            var query = new GetSessionHistoryQuery(SessionId.Create(sessionId), limit, offset);
            var result = await _mediator.Send(query, cancellationToken);
            
            if (result == null)
            {
                return NotFound(new { Message = "Session not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving history for session {SessionId}", sessionId);
            return StatusCode(500, new { Message = "Failed to retrieve history", Error = ex.Message });
        }
    }

    /// <summary>
    /// Gets all sessions for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="limit">Maximum number of sessions to return</param>
    /// <param name="offset">Number of sessions to skip</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user sessions</returns>
    [HttpGet("users/{userId}/sessions")]
    [SwaggerOperation(
        Summary = "Get user sessions",
        Description = "Retrieves all chat sessions for a specific user with pagination"
    )]
    [SwaggerResponse(200, "Sessions retrieved", typeof(GetSessionsResponse))]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<GetSessionsResponse>> GetUserSessions(
        [FromRoute] string userId,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetSessionsQuery(userId, limit, offset);
            var result = await _mediator.Send(query, cancellationToken);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions for user {UserId}", userId);
            return StatusCode(500, new { Message = "Failed to retrieve sessions", Error = ex.Message });
        }
    }
}

// DTOs for API contracts
public record CreateSessionRequest
{
    public string? UserId { get; init; }
    public string? Context { get; init; }
}

public record CreateSessionResponse
{
    public required string SessionId { get; init; }
    public required string UserId { get; init; }
    public required string Context { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public record SendMessageRequest
{
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public required string Message { get; init; }
    
    public bool? IncludeContext { get; init; }
    
    [Range(100, 4000)]
    public int? MaxTokens { get; init; }
    
    [Range(0.0, 2.0)]
    public float? Temperature { get; init; }
}

public record SendMessageResponse
{
    public required string SessionId { get; init; }
    public required string MessageId { get; init; }
    public required string Response { get; init; }
    public required List<ContextItem> Context { get; init; }
    public required int TokensUsed { get; init; }
    public required long ProcessingTimeMs { get; init; }
    public required DateTime Timestamp { get; init; }
}

public record ContextItem
{
    public required string Source { get; init; }
    public required string Content { get; init; }
    public required double Score { get; init; }
}

public record GetSessionResponse
{
    public required string SessionId { get; init; }
    public required string UserId { get; init; }
    public required string Context { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime LastActivityAt { get; init; }
    public required int MessageCount { get; init; }
}

public record GetSessionsResponse
{
    public required List<SessionSummary> Sessions { get; init; }
    public required int TotalCount { get; init; }
    public required bool HasMore { get; init; }
}

public record SessionSummary
{
    public required string SessionId { get; init; }
    public required string Context { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime LastActivityAt { get; init; }
    public required int MessageCount { get; init; }
}

public record GetSessionHistoryResponse
{
    public required string SessionId { get; init; }
    public required List<ChatMessage> Messages { get; init; }
    public required int TotalCount { get; init; }
    public required bool HasMore { get; init; }
}

public record ChatMessage
{
    public required string MessageId { get; init; }
    public required string Content { get; init; }
    public required bool IsFromUser { get; init; }
    public required DateTime Timestamp { get; init; }
    public List<ContextItem>? Context { get; init; }
}
