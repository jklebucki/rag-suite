using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using RAG.Security.DTOs;
using RAG.Security.Models;

namespace RAG.Security.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user, IList<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    TokenValidationResponse ValidateToken(string token);
    Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);
    Task SaveRefreshTokenAsync(string userId, string refreshToken);
    Task RevokeRefreshTokenAsync(string userId, string refreshToken);
    Task RevokeAllRefreshTokensAsync(string userId);
}