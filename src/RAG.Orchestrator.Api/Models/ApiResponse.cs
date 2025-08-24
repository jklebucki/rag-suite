namespace RAG.Orchestrator.Api.Models;

public record ApiResponse<T>(
    T Data,
    bool Success = true,
    string? Message = null,
    string[]? Errors = null
);

public static class ApiResponseExtensions
{
    public static IResult ToApiResponse<T>(this T data, string? message = null)
    {
        var response = new ApiResponse<T>(data, true, message);
        return Results.Ok(response);
    }

    public static IResult ToApiErrorResponse<T>(string message, string[]? errors = null)
    {
        var response = new ApiResponse<T>(default!, false, message, errors);
        return Results.BadRequest(response);
    }

    public static IResult ToApiNotFoundResponse<T>(string? message = null)
    {
        var response = new ApiResponse<T>(default!, false, message ?? "Resource not found");
        return Results.NotFound(response);
    }

    public static IResult ToApiCreatedResponse<T>(this T data, string location, string? message = null)
    {
        var response = new ApiResponse<T>(data, true, message);
        return Results.Created(location, response);
    }
}
