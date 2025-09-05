using Microsoft.AspNetCore.Mvc;
using RAG.Security.Models;
using RAG.Security.Services;

namespace RAG.Security.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly IRegistrationConfigurationService _configurationService;

    public ConfigurationController(IRegistrationConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    /// <summary>
    /// Gets the current registration configuration including password requirements and field validation rules
    /// </summary>
    /// <returns>Registration configuration object</returns>
    [HttpGet("registration")]
    public ActionResult<RegistrationConfiguration> GetRegistrationConfiguration()
    {
        try
        {
            var configuration = _configurationService.GetConfiguration();
            return Ok(configuration);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to retrieve configuration", error = ex.Message });
        }
    }
}
