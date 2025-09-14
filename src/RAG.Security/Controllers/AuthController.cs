using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RAG.Security.DTOs;
using RAG.Security.Services;
using System.Security.Claims;

namespace RAG.Security.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;

    public AuthController(IAuthService authService, IJwtService jwtService)
    {
        _authService = authService;
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await _authService.LoginAsync(request);
        if (response == null)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        return Ok(response);
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _authService.RegisterAsync(request);
        if (!success)
        {
            return BadRequest(new { message = "User with this email or username already exists" });
        }

        return Ok(new { message = "User registered successfully" });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await _authService.RefreshTokenAsync(request);
        if (response == null)
        {
            return Unauthorized(new { message = "Invalid refresh token" });
        }

        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _authService.LogoutAsync(userId, request.RefreshToken);
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("logout-all-devices")]
    [Authorize]
    public async Task<ActionResult> LogoutAllDevices()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _authService.LogoutAllDevicesAsync(userId);
        return Ok(new { message = "Logged out from all devices successfully" });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var success = await _authService.ChangePasswordAsync(userId, request);
        if (!success)
        {
            return BadRequest(new { message = "Failed to change password. Check your current password." });
        }

        return Ok(new { message = "Password changed successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var userInfo = await _authService.GetUserInfoAsync(userId);
        if (userInfo == null)
        {
            return NotFound();
        }

        return Ok(userInfo);
    }

    [HttpPost("validate-token")]
    public ActionResult<TokenValidationResponse> ValidateToken([FromBody] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { message = "Token is required" });
        }

        var response = _jwtService.ValidateToken(token);
        return Ok(response);
    }

    [HttpPost("assign-role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> AssignRole([FromBody] AssignRoleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _authService.AssignRoleAsync(request.UserId, request.RoleName);
        if (!success)
        {
            return BadRequest(new { message = "Failed to assign role" });
        }

        return Ok(new { message = "Role assigned successfully" });
    }

    [HttpPost("remove-role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> RemoveRole([FromBody] AssignRoleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _authService.RemoveRoleAsync(request.UserId, request.RoleName);
        if (!success)
        {
            return BadRequest(new { message = "Failed to remove role" });
        }

        return Ok(new { message = "Role removed successfully" });
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _authService.ForgotPasswordAsync(request.Email);
        if (!success)
        {
            return BadRequest(new { message = "Failed to process password reset request" });
        }

        return Ok(new { message = "If the email exists, a password reset link has been sent" });
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _authService.ResetPasswordAsync(request);
        if (!success)
        {
            return BadRequest(new { message = "Invalid or expired reset token" });
        }

        return Ok(new { message = "Password has been reset successfully" });
    }
}

public record AssignRoleRequest
{
    public string UserId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
}
