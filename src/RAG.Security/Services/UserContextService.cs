using Microsoft.AspNetCore.Http;

namespace RAG.Security.Services;

public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.Items["UserId"] as string;
    }

    public string? GetCurrentUserName()
    {
        return _httpContextAccessor.HttpContext?.Items["UserName"] as string;
    }

    public string? GetCurrentUserEmail()
    {
        return _httpContextAccessor.HttpContext?.Items["UserEmail"] as string;
    }

    public string[] GetCurrentUserRoles()
    {
        return _httpContextAccessor.HttpContext?.Items["UserRoles"] as string[] ?? Array.Empty<string>();
    }

    public bool IsAuthenticated()
    {
        return !string.IsNullOrEmpty(GetCurrentUserId());
    }

    public bool IsInRole(string role)
    {
        var userRoles = GetCurrentUserRoles();
        return userRoles.Contains(role);
    }

    public bool HasAnyRole(params string[] roles)
    {
        var userRoles = GetCurrentUserRoles();
        return roles.Any(role => userRoles.Contains(role));
    }
}
