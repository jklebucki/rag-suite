using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace RAG.Security.Middleware;

public class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = context.User.FindFirst(ClaimTypes.Name)?.Value;
            var userEmail = context.User.FindFirst(ClaimTypes.Email)?.Value;
            var userRoles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

            // Add user context to HttpContext.Items for easy access in services
            context.Items["UserId"] = userId;
            context.Items["UserName"] = userName;
            context.Items["UserEmail"] = userEmail;
            context.Items["UserRoles"] = userRoles;
        }

        await _next(context);
    }
}

public static class UserContextMiddlewareExtensions
{
    public static IApplicationBuilder UseUserContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserContextMiddleware>();
    }
}
