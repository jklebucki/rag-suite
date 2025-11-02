using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.AddressBook.Features.ImportContacts;

public static class ImportContactsEndpoint
{
    public static RouteHandlerBuilder MapImportContactsEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapPost("/import", ImportContactsAsync)
            .WithName("ImportContacts")
            .WithTags("AddressBook")
            .WithOpenApi()
            .RequireAuthorization("AdminOrPowerUser")
            .DisableAntiforgery(); // For file upload
    }

    private static async Task<IResult> ImportContactsAsync(
        [FromBody] ImportContactsRequest request,
        [FromServices] ImportContactsHandler handler,
        [FromServices] IValidator<ImportContactsRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var response = await handler.HandleAsync(request, cancellationToken);
        return Results.Ok(response);
    }
}
