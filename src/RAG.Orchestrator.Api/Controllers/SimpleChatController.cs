using Microsoft.AspNetCore.Mvc;
using RAG.Application.Services;

namespace RAG.Orchestrator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimpleChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<SimpleChatController> _logger;

    public SimpleChatController(IChatService chatService, ILogger<SimpleChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new chat session
    /// </summary>
    [HttpPost("sessions")]
    public async Task<ActionResult<CreateSessionResponse>> CreateSession([FromBody] CreateSessionRequest request)
    {
        try
        {
            var response = await _chatService.CreateSessionAsync(request);
            return CreatedAtAction(nameof(CreateSession), response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session for user {UserId}", request.UserId);
            return StatusCode(500, new { Message = "Failed to create session", Error = ex.Message });
        }
    }

    /// <summary>
    /// Sends a message with Semantic Kernel processing
    /// </summary>
    [HttpPost("messages")]
    public async Task<ActionResult<SendMessageResponse>> SendMessage([FromBody] SendMessageRequest request)
    {
        try
        {
            var response = await _chatService.SendMessageAsync(request);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument for session {SessionId}", request.SessionId);
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for session {SessionId}", request.SessionId);
            return StatusCode(500, new { Message = "Failed to process message", Error = ex.Message });
        }
    }
}
