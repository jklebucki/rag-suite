using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RAG.Security.DTOs;
using RAG.Security.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RAG.Security.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpiryMinutes;
    private readonly int _refreshTokenExpiryDays;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DateTime>> _refreshTokens = new();

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
        _secretKey = configuration["Jwt:SecretKey"] ?? "your-super-secret-key-that-is-at-least-256-bits-long-for-security";
        _issuer = configuration["Jwt:Issuer"] ?? "RAG.Suite";
        _audience = configuration["Jwt:Audience"] ?? "RAG.Suite.Client";
        _accessTokenExpiryMinutes = configuration.GetValue<int>("Jwt:AccessTokenExpiryMinutes", 60);
        _refreshTokenExpiryDays = configuration.GetValue<int>("Jwt:RefreshTokenExpiryDays", 7);
    }

    public string GenerateAccessToken(User user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("firstName", user.FirstName),
            new("lastName", user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // Don't validate lifetime for refresh token scenario
            ValidateIssuerSigningKey = true,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public TokenValidationResponse ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

            return new TokenValidationResponse
            {
                IsValid = true,
                UserId = userId,
                Roles = roles
            };
        }
        catch (SecurityTokenExpiredException)
        {
            return new TokenValidationResponse
            {
                IsValid = false,
                ErrorMessage = "Token has expired"
            };
        }
        catch (SecurityTokenException ex)
        {
            return new TokenValidationResponse
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            return new TokenValidationResponse
            {
                IsValid = false,
                ErrorMessage = $"Token validation failed: {ex.Message}"
            };
        }
    }

    public Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
    {
        if (!_refreshTokens.TryGetValue(userId, out var tokens))
        {
            return Task.FromResult(false);
        }

        CleanupExpiredTokens(userId, tokens);

        if (tokens.TryGetValue(refreshToken, out var expiresAt))
        {
            if (expiresAt > DateTime.UtcNow)
            {
                return Task.FromResult(true);
            }

            tokens.TryRemove(refreshToken, out _);
            CleanupUserEntryIfEmpty(userId, tokens);
        }

        return Task.FromResult(false);
    }

    public Task<string?> FindUserIdByRefreshTokenAsync(string refreshToken)
    {
        foreach (var kvp in _refreshTokens)
        {
            var userId = kvp.Key;
            var tokens = kvp.Value;

            CleanupExpiredTokens(userId, tokens);

            if (tokens.ContainsKey(refreshToken))
            {
                if (tokens.TryGetValue(refreshToken, out var expiresAt) && expiresAt > DateTime.UtcNow)
                {
                    return Task.FromResult<string?>(userId);
                }

                tokens.TryRemove(refreshToken, out _);
                CleanupUserEntryIfEmpty(userId, tokens);
            }
        }

        return Task.FromResult<string?>(null);
    }

    public Task SaveRefreshTokenAsync(string userId, string refreshToken)
    {
        var tokenStore = _refreshTokens.GetOrAdd(userId, _ => new ConcurrentDictionary<string, DateTime>());

        CleanupExpiredTokens(userId, tokenStore);

        var expiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpiryDays);
        tokenStore[refreshToken] = expiresAt;

        return Task.CompletedTask;
    }

    public Task RevokeRefreshTokenAsync(string userId, string refreshToken)
    {
        if (_refreshTokens.TryGetValue(userId, out var tokens))
        {
            tokens.TryRemove(refreshToken, out _);
            CleanupUserEntryIfEmpty(userId, tokens);
        }

        return Task.CompletedTask;
    }

    public Task RevokeAllRefreshTokensAsync(string userId)
    {
        _refreshTokens.TryRemove(userId, out _);
        return Task.CompletedTask;
    }

    private void CleanupExpiredTokens(string userId, ConcurrentDictionary<string, DateTime> tokens)
    {
        var now = DateTime.UtcNow;

        foreach (var token in tokens)
        {
            if (token.Value <= now)
            {
                tokens.TryRemove(token.Key, out _);
            }
        }

        CleanupUserEntryIfEmpty(userId, tokens);
    }

    private void CleanupUserEntryIfEmpty(string userId, ConcurrentDictionary<string, DateTime> tokens)
    {
        if (tokens.IsEmpty)
        {
            _refreshTokens.TryRemove(userId, out _);
        }
    }
}
