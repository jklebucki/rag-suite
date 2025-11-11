using FluentAssertions;
using Moq;
using RAG.AddressBook.Services;
using RAG.Security.Services;

namespace RAG.Tests.AddressBook;

public class AddressBookAuthorizationServiceTests
{
    private readonly Mock<IUserContextService> _mockUserContext;
    private readonly AddressBookAuthorizationService _service;

    public AddressBookAuthorizationServiceTests()
    {
        _mockUserContext = new Mock<IUserContextService>();
        _service = new AddressBookAuthorizationService(_mockUserContext.Object);
    }

    [Fact]
    public void CanModifyContacts_WithAdminRole_ReturnsTrue()
    {
        // Arrange
        _mockUserContext.Setup(u => u.HasAnyRole("Admin", "PowerUser")).Returns(true);

        // Act
        var result = _service.CanModifyContacts();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanModifyContacts_WithPowerUserRole_ReturnsTrue()
    {
        // Arrange
        _mockUserContext.Setup(u => u.HasAnyRole("Admin", "PowerUser")).Returns(true);

        // Act
        var result = _service.CanModifyContacts();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanModifyContacts_WithoutAdminOrPowerUser_ReturnsFalse()
    {
        // Arrange
        _mockUserContext.Setup(u => u.HasAnyRole("Admin", "PowerUser")).Returns(false);

        // Act
        var result = _service.CanModifyContacts();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAdminOrPowerUser_WithAdminRole_ReturnsTrue()
    {
        // Arrange
        _mockUserContext.Setup(u => u.HasAnyRole("Admin", "PowerUser")).Returns(true);

        // Act
        var result = _service.IsAdminOrPowerUser();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAdminOrPowerUser_WithoutAdminOrPowerUser_ReturnsFalse()
    {
        // Arrange
        _mockUserContext.Setup(u => u.HasAnyRole("Admin", "PowerUser")).Returns(false);

        // Act
        var result = _service.IsAdminOrPowerUser();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetCurrentUserId_WithUserId_ReturnsUserId()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns("user123");

        // Act
        var result = _service.GetCurrentUserId();

        // Assert
        result.Should().Be("user123");
    }

    [Fact]
    public void GetCurrentUserId_WithoutUserId_ReturnsSystem()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserId()).Returns((string?)null);

        // Act
        var result = _service.GetCurrentUserId();

        // Assert
        result.Should().Be("system");
    }

    [Fact]
    public void GetCurrentUserName_WithUserName_ReturnsUserName()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserName()).Returns("john.doe");

        // Act
        var result = _service.GetCurrentUserName();

        // Assert
        result.Should().Be("john.doe");
    }

    [Fact]
    public void GetCurrentUserName_WithoutUserName_ReturnsNull()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetCurrentUserName()).Returns((string?)null);

        // Act
        var result = _service.GetCurrentUserName();

        // Assert
        result.Should().BeNull();
    }
}

