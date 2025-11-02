using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.AddressBook.Features.ProposeChange;

public static class ProposeChangeEndpoint
{
    public static RouteHandlerBuilder MapProposeChangeEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapPost("/proposals", ProposeChangeAsync)
            .WithName("ProposeContactChange")
            .WithTags("AddressBook", "Proposals")
            .WithOpenApi()
            .RequireAuthorization();
    }

    private static async Task<IResult> ProposeChangeAsync(
        [FromBody] ProposeContactChangeRequest request,
        [FromServices] ProposeChangeHandler handler,
        [FromServices] IValidator<ProposeContactChangeRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            var response = await handler.HandleAsync(request, cancellationToken);
            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
