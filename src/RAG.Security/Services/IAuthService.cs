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
    Task<PasswordOperationResult> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<UserInfo?> GetUserInfoAsync(string userId);
    Task<bool> AssignRoleAsync(string userId, string roleName);
    Task<bool> RemoveRoleAsync(string userId, string roleName);
    Task<IList<string>> GetUserRolesAsync(string userId);
    Task<bool> ForgotPasswordAsync(string email, string uiUrl);
    Task<PasswordOperationResult> ResetPasswordAsync(ResetPasswordRequest request);
    Task<PasswordOperationResult> SetPasswordAsync(string userId, string newPassword);
    Task<List<UserInfo>> GetAllUsersAsync();
    Task<List<string>> GetAllRolesAsync();
    Task<bool> DeleteUserAsync(string userId);
}

public record PasswordOperationResult(bool Succeeded, string[] Errors)
{
    public static PasswordOperationResult Success() => new(true, Array.Empty<string>());
    public static PasswordOperationResult Failure(params string[] errors) => new(false, errors);
}
