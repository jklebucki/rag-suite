using System.Collections.Generic;
using System.Threading.Tasks;
using RAG.Security.DTOs;
using RAG.Security.Requests;

namespace RAG.Security.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<bool> RegisterAsync(RegisterRequest request);
    Task<LoginResponse?> RefreshTokenAsync(RefreshTokenRequest request);
    Task<bool> LogoutAsync(string userId, string refreshToken);
    Task<bool> LogoutAllDevicesAsync(string userId);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<UserInfo?> GetUserInfoAsync(string userId);
    Task<bool> AssignRoleAsync(string userId, string roleName);
    Task<bool> RemoveRoleAsync(string userId, string roleName);
    Task<IList<string>> GetUserRolesAsync(string userId);
    Task<bool> ForgotPasswordAsync(string email, string uiUrl);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
    Task<bool> SetPasswordAsync(string userId, string newPassword);
}