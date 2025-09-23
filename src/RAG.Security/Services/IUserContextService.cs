namespace RAG.Security.Services;

public interface IUserContextService
{
    string? GetCurrentUserId();
    string? GetCurrentUserName();
    string? GetCurrentUserEmail();
    string[] GetCurrentUserRoles();
    bool IsAuthenticated();
    bool IsInRole(string role);
    bool HasAnyRole(params string[] roles);
}