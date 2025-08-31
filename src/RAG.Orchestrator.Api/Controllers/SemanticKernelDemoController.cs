using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;

namespace RAG.Orchestrator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SemanticKernelDemoController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly ILogger<SemanticKernelDemoController> _logger;

    public SemanticKernelDemoController(Kernel kernel, ILogger<SemanticKernelDemoController> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    /// <summary>
    /// Demo endpoint pokazujący działanie Semantic Kernel z Ollama
    /// </summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        try
        {
            _logger.LogInformation("Processing chat request: {Message}", request.Message);

            // Prosty prompt dla Ollama
            var prompt = $"""
                You are a helpful AI assistant. Please respond to the following question in a clear and concise manner:
                
                Question: {request.Message}
                
                Answer:
                """;

            // Wywołanie Semantic Kernel
            var response = await _kernel.InvokePromptAsync(prompt);
            
            var result = new ChatResponse
            {
                Message = request.Message,
                Response = response.GetValue<string>() ?? "No response generated",
                Timestamp = DateTime.UtcNow,
                Model = "ollama-llama3.2"
            };

            _logger.LogInformation("Successfully processed chat request");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request: {Message}", request.Message);
            return StatusCode(500, new { 
                Error = "Failed to process request", 
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Health check dla Semantic Kernel
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> HealthCheck()
    {
        try
        {
            // Test connection to Ollama
            var testResponse = await _kernel.InvokePromptAsync("Say 'hello' in one word.");
            
            return Ok(new {
                Status = "Healthy",
                SemanticKernel = "Connected",
                Ollama = "Available",
                TestResponse = testResponse.GetValue<string>(),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new {
                Status = "Unhealthy",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}

public record ChatRequest
{
    public required string Message { get; init; }
}

public record ChatResponse
{
    public required string Message { get; init; }
    public required string Response { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Model { get; init; }
}
