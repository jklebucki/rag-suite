using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RAG.Security.Data;
using RAG.Security.DTOs;
using RAG.Security.Models;
using System.Linq;

namespace RAG.Security.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<bool> RegisterAsync(RegisterRequest request);
    Task<LoginResponse?> RefreshTokenAsync(RefreshTokenRequest request);
    Task<bool> LogoutAsync(string userId, string refreshToken);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<UserInfo?> GetUserInfoAsync(string userId);
    Task<bool> AssignRoleAsync(string userId, string roleName);
    Task<bool> RemoveRoleAsync(string userId, string roleName);
    Task<IList<string>> GetUserRolesAsync(string userId);
    Task<bool> ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
}

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly SecurityDbContext _context;
    private readonly IEmailService _emailService;

    public AuthService(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        SignInManager<User> signInManager,
        IJwtService jwtService,
        SecurityDbContext context,
        IEmailService emailService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _context = context;
        _emailService = emailService;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            return null;
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
        {
            return null;
        }

        // Update last login time
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtService.GenerateAccessToken(user, roles);
        var refreshToken = _jwtService.GenerateRefreshToken();

        await _jwtService.SaveRefreshTokenAsync(user.Id, refreshToken);

        return new LoginResponse
        {
            Token = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60), // Should match JWT config
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName ?? string.Empty,
                Roles = roles.ToArray(),
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            }
        };
    }

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return false;
        }

        existingUser = await _userManager.FindByNameAsync(request.UserName);
        if (existingUser != null)
        {
            return false;
        }

        var user = new User
        {
            Email = request.Email,
            UserName = request.UserName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true, // For simplicity, auto-confirm emails
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return false;
        }

        // Assign default User role
        await _userManager.AddToRoleAsync(user, Models.UserRoles.User);

        return true;
    }

    public async Task<LoginResponse?> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = _jwtService.GetPrincipalFromExpiredToken(request.RefreshToken);
        if (principal == null)
        {
            return null;
        }

        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var isValidRefreshToken = await _jwtService.ValidateRefreshTokenAsync(userId, request.RefreshToken);
        if (!isValidRefreshToken)
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _jwtService.GenerateAccessToken(user, roles);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        // Revoke old refresh token and save new one
        await _jwtService.RevokeRefreshTokenAsync(userId, request.RefreshToken);
        await _jwtService.SaveRefreshTokenAsync(userId, newRefreshToken);

        return new LoginResponse
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName ?? string.Empty,
                Roles = roles.ToArray(),
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            }
        };
    }

    public async Task<bool> LogoutAsync(string userId, string refreshToken)
    {
        await _jwtService.RevokeRefreshTokenAsync(userId, refreshToken);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        return result.Succeeded;
    }

    public async Task<UserInfo?> GetUserInfoAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new UserInfo
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            UserName = user.UserName ?? string.Empty,
            Roles = roles.ToArray(),
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    public async Task<bool> AssignRoleAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var roleExists = await _roleManager.RoleExistsAsync(roleName);
        if (!roleExists)
        {
            return false;
        }

        var result = await _userManager.AddToRoleAsync(user, roleName);
        return result.Succeeded;
    }

    public async Task<bool> RemoveRoleAsync(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        return result.Succeeded;
    }

    public async Task<IList<string>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new List<string>();
        }

        return await _userManager.GetRolesAsync(user);
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !user.IsActive)
        {
            // Return true to avoid revealing if email exists
            return true;
        }

        // Generate reset token
        var resetToken = Guid.NewGuid().ToString();
        var passwordResetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = resetToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1) // Token valid for 1 hour
        };

        _context.PasswordResetTokens.Add(passwordResetToken);
        await _context.SaveChangesAsync();

        // Generate reset link (assuming frontend URL)
        var resetLink = $"https://yourapp.com/reset-password?token={resetToken}";

        // Send email
        await _emailService.SendPasswordResetEmailAsync(email, resetLink);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var passwordResetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(prt => prt.Token == request.Token && !prt.IsUsed && prt.ExpiresAt > DateTime.UtcNow);

        if (passwordResetToken == null)
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(passwordResetToken.UserId);
        if (user == null || !user.IsActive)
        {
            return false;
        }

        // Reset password
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

        if (result.Succeeded)
        {
            // Mark token as used
            passwordResetToken.IsUsed = true;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }
}
