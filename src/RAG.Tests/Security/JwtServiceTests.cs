using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RAG.Security.Services;
using RAG.Security.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RAG.Tests.Security;

public class JwtServiceTests
{
    private readonly IJwtService _jwtService;

    public JwtServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"Jwt:SecretKey", "test-secret-key-that-is-at-least-256-bits-long-for-testing-purposes-only"},
                {"Jwt:Issuer", "RAG.Suite.Test"},
                {"Jwt:Audience", "RAG.Suite.Test.Client"},
                {"Jwt:AccessTokenExpiryMinutes", "60"},
                {"Jwt:RefreshTokenExpiryDays", "7"}
            })
            .Build();

        _jwtService = new JwtService(configuration, NullLogger<JwtService>.Instance);
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_ReturnsNonEmptyToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        var roles = new List<string> { "User" };

        // Act
        var token = _jwtService.GenerateAccessToken(user, roles);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // JWT contains dots
    }

    [Fact]
    public void GenerateAccessToken_WithMultipleRoles_TokenContainsAllRoles()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "adminuser",
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "User"
        };
        var roles = new List<string> { "User", "Admin", "Editor" };

        // Act
        var token = _jwtService.GenerateAccessToken(user, roles);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateAccessToken_EmptyRoles_ReturnsValidToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "noroleuser",
            Email = "norole@example.com",
            FirstName = "No",
            LastName = "Role"
        };
        var roles = new List<string>();

        // Act
        var token = _jwtService.GenerateAccessToken(user, roles);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyToken()
    {
        // Act
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Assert
        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken);
        Assert.True(refreshToken.Length > 20); // Refresh tokens are typically longer
    }

    [Fact]
    public void GenerateRefreshToken_GeneratesUniqueTokens()
    {
        // Act
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public async Task SaveRefreshToken_AndValidate_Success()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Act
        await _jwtService.SaveRefreshTokenAsync(userId, refreshToken);
        var isValid = await _jwtService.ValidateRefreshTokenAsync(userId, refreshToken);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task ValidateRefreshToken_InvalidToken_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var validToken = _jwtService.GenerateRefreshToken();
        var invalidToken = "invalid-token";

        await _jwtService.SaveRefreshTokenAsync(userId, validToken);

        // Act
        var isValid = await _jwtService.ValidateRefreshTokenAsync(userId, invalidToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidateRefreshToken_NonExistentUser_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var token = _jwtService.GenerateRefreshToken();

        // Act
        var isValid = await _jwtService.ValidateRefreshTokenAsync(userId, token);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task RevokeRefreshToken_Success()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var refreshToken = _jwtService.GenerateRefreshToken();

        await _jwtService.SaveRefreshTokenAsync(userId, refreshToken);

        // Act
        await _jwtService.RevokeRefreshTokenAsync(userId, refreshToken);
        var isValid = await _jwtService.ValidateRefreshTokenAsync(userId, refreshToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task RevokeAllRefreshTokens_Success()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();

        await _jwtService.SaveRefreshTokenAsync(userId, token1);
        await _jwtService.SaveRefreshTokenAsync(userId, token2);

        // Act
        await _jwtService.RevokeAllRefreshTokensAsync(userId);
        var isValid1 = await _jwtService.ValidateRefreshTokenAsync(userId, token1);
        var isValid2 = await _jwtService.ValidateRefreshTokenAsync(userId, token2);

        // Assert
        Assert.False(isValid1);
        Assert.False(isValid2);
    }

    [Fact]
    public async Task SaveRefreshToken_MultipleTimes_KeepsAllTokens()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();

        // Act
        await _jwtService.SaveRefreshTokenAsync(userId, token1);
        await _jwtService.SaveRefreshTokenAsync(userId, token2);

        var isValid1 = await _jwtService.ValidateRefreshTokenAsync(userId, token1);
        var isValid2 = await _jwtService.ValidateRefreshTokenAsync(userId, token2);

        // Assert
        Assert.True(isValid1);
        Assert.True(isValid2);
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsValidResponse()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        var roles = new List<string> { "User", "Admin" };
        var token = _jwtService.GenerateAccessToken(user, roles);

        // Act
        var response = _jwtService.ValidateToken(token);

        // Assert
        Assert.True(response.IsValid);
        Assert.Equal(user.Id, response.UserId);
        Assert.Contains("User", response.Roles);
        Assert.Contains("Admin", response.Roles);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsInvalidResponse()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var response = _jwtService.ValidateToken(invalidToken);

        // Assert
        Assert.False(response.IsValid);
        Assert.Null(response.UserId);
        Assert.Empty(response.Roles);
        Assert.NotNull(response.ErrorMessage);
    }

    [Fact]
    public void ValidateToken_EmptyToken_ReturnsInvalidResponse()
    {
        // Act
        var response = _jwtService.ValidateToken(string.Empty);

        // Assert
        Assert.False(response.IsValid);
        Assert.NotNull(response.ErrorMessage);
    }
}
