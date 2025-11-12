using Microsoft.AspNetCore.Http;
using RAG.Abstractions.Common.Api;

namespace RAG.Forum.Common;

public static class ApiResponseExtensions
{
    public static IResult ToApiResponse<T>(this T data, string? message = null)
    {
        var response = new ApiResponse<T>(data, true, message);
        return Microsoft.AspNetCore.Http.Results.Ok(response);
    }

    public static IResult ToApiErrorResponse<T>(string message, string[]? errors = null)
    {
        var response = new ApiResponse<T>(default!, false, message, errors);
        return Microsoft.AspNetCore.Http.Results.BadRequest(response);
    }

    public static IResult ToApiNotFoundResponse<T>(string? message = null)
    {
        var response = new ApiResponse<T>(default!, false, message ?? "Resource not found");
        return Microsoft.AspNetCore.Http.Results.NotFound(response);
    }

    public static IResult ToApiCreatedResponse<T>(this T data, string location, string? message = null)
    {
        var response = new ApiResponse<T>(data, true, message);
        return Microsoft.AspNetCore.Http.Results.Created(location, response);
    }
}

