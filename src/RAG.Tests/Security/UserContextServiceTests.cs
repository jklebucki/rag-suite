using Xunit;
using Microsoft.AspNetCore.Http;
using Moq;
using RAG.Security.Services;
using System.Collections.Generic;

namespace RAG.Tests.Security;

public class UserContextServiceTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly UserContextService _service;

    public UserContextServiceTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _service = new UserContextService(_mockHttpContextAccessor.Object);
    }

    [Fact]
    public void GetCurrentUserId_WhenUserIdExists_ReturnsUserId()
    {
        // Arrange
        var userId = "user-123";
        var httpContext = new DefaultHttpContext();
        httpContext.Items["UserId"] = userId;
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _service.GetCurrentUserId();

        // Assert
        Assert.Equal(userId, result);
    }

    [Fact]
    public void GetCurrentUserId_WhenNoHttpContext_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _service.GetCurrentUserId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentUserId_WhenUserIdNotSet_ReturnsNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _service.GetCurrentUserId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentUserName_WhenUserNameExists_ReturnsUserName()
    {
        // Arrange
        var userName = "testuser";
        var httpContext = new DefaultHttpContext();
        httpContext.Items["UserName"] = userName;
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _service.GetCurrentUserName();

        // Assert
        Assert.Equal(userName, result);
    }

    [Fact]
    public void GetCurrentUserEmail_WhenEmailExists_ReturnsEmail()
    {
        // Arrange
        var email = "test@example.com";
        var httpContext = new DefaultHttpContext();
        httpContext.Items["UserEmail"] = email;
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _service.GetCurrentUserEmail();

        // Assert
        Assert.Equal(email, result);
    }

    [Fact]
    public void GetCurrentUserRoles_WhenRolesExist_ReturnsRoles()
    {
        // Arrange
        var roles = new[] { "Admin", "User" };
        var httpContext = new DefaultHttpContext();
        httpContext.Items["UserRoles"] = roles;
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _service.GetCurrentUserRoles();

        // Assert
        Assert.Equal(roles, result);
        Assert.Equal(2, result.Length);
        Assert.Contains("Admin", result);
        Assert.Contains("User", result);
    }

    [Fact]
    public void GetCurrentUserRoles_WhenNoRoles_ReturnsEmptyArray()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _service.GetCurrentUserRoles();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetCurrentUserRoles_WhenNoHttpContext_ReturnsEmptyArray()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        var result = _service.GetCurrentUserRoles();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void IsAuthenticated_WhenUserIdExists_ReturnsTrue()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["UserId"] = "user-123";
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _service.IsAuthenticated();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAuthenticated_WhenUserIdIsEmpty_ReturnsFalse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["UserId"] = string.Empty;
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _service.IsAuthenticated();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAuthenticated_WhenUserIdIsNull_ReturnsFalse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _service.IsAuthenticated();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsInRole_WhenUserHasRole_ReturnsTrue()
    {
        // Arrange
        var roles = new[] { "Admin", "User" };
        var httpContext = new DefaultHttpContext();
        httpContext.Items["UserRoles"] = roles;
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _service.IsInRole("Admin");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInRole_WhenUserDoesNotHaveRole_ReturnsFalse()
    {
        // Arrange
        var roles = new[] { "User" };
        var httpContext = new DefaultHttpContext();
        httpContext.Items["UserRoles"] = roles;
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _service.IsInRole("Admin");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsInRole_WhenNoRoles_ReturnsFalse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _service.IsInRole("Admin");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasAnyRole_WhenUserHasOneOfRoles_ReturnsTrue()
    {
        // Arrange
        var roles = new[] { "User", "Editor" };
        var httpContext = new DefaultHttpContext();
        httpContext.Items["UserRoles"] = roles;
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _service.HasAnyRole("Admin", "Editor", "Viewer");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasAnyRole_WhenUserHasNoneOfRoles_ReturnsFalse()
    {
        // Arrange
        var roles = new[] { "User" };
        var httpContext = new DefaultHttpContext();
        httpContext.Items["UserRoles"] = roles;
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _service.HasAnyRole("Admin", "Editor", "Viewer");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasAnyRole_WhenNoRolesProvided_ReturnsFalse()
    {
        // Arrange
        var roles = new[] { "User" };
        var httpContext = new DefaultHttpContext();
        httpContext.Items["UserRoles"] = roles;
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _service.HasAnyRole();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasAnyRole_WhenUserHasNoRoles_ReturnsFalse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = _service.HasAnyRole("Admin", "User");

        // Assert
        Assert.False(result);
    }
}
