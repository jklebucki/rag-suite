using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace RAG.AddressBook.Features.ReviewProposal;

public static class ReviewProposalEndpoint
{
    public static RouteHandlerBuilder MapReviewProposalEndpoint(this IEndpointRouteBuilder app)
    {
        return app.MapPost("/proposals/{id:guid}/review", ReviewProposalAsync)
            .WithName("ReviewProposal")
            .WithTags("AddressBook", "Proposals")
            .WithOpenApi()
            .RequireAuthorization("AdminOrPowerUser");
    }

    private static async Task<IResult> ReviewProposalAsync(
        Guid id,
        [FromBody] ReviewProposalRequest request,
        [FromServices] ReviewProposalHandler handler,
        [FromServices] IValidator<ReviewProposalRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        try
        {
            var response = await handler.HandleAsync(id, request, cancellationToken);
            return Results.Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
