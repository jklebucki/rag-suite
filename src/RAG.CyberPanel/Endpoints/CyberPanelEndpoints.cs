using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using RAG.CyberPanel.Data;
using RAG.CyberPanel.Services;

namespace RAG.CyberPanel.Endpoints;

public static class CyberPanelEndpoints
{
    public static RouteGroupBuilder MapCyberPanelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/cyberpanel").WithTags("CyberPanel").WithOpenApi();

        group.MapGet("/quizzes", async ([FromServices] CyberPanelDbContext db, HttpContext ctx) =>
        {
            var quizzes = await db.Quizzes.AsNoTracking().Select(q => new { q.Id, q.Title, q.Description, q.IsPublished }).ToListAsync(ctx.RequestAborted);
            return Results.Ok(quizzes);
        }).RequireAuthorization();

        group.MapGet("/quizzes/{id}", async (Guid id, [FromServices] CyberPanelDbContext db, HttpContext ctx) =>
        {
            var quiz = await db.Quizzes.Include(q => q.Questions).ThenInclude(qn => qn.Options).FirstOrDefaultAsync(q => q.Id == id, ctx.RequestAborted);
            if (quiz == null) return Results.NotFound();

            // For simplicity, do not return IsCorrect in options via this scaffold
            var dto = new
            {
                quiz.Id,
                quiz.Title,
                quiz.Description,
                quiz.IsPublished,
                Questions = quiz.Questions.Select(q => new
                {
                    q.Id,
                    q.Text,
                    q.Points,
                    Options = q.Options.Select(o => new { o.Id, o.Text })
                })
            };

            return Results.Ok(dto);
        }).RequireAuthorization();

        group.MapPost("/quizzes/{id}/attempts", [Authorize] async (Guid id, [FromBody] object body, [FromServices] CyberPanelDbContext db, [FromServices] ICyberPanelService service, HttpContext ctx) =>
        {
            // Scaffolding: accept body and respond with 201; real implementation will validate and compute score
            // TODO: implement parsing of AttemptSubmitDto and scoring logic
            if (await db.Quizzes.AnyAsync(q => q.Id == id, ctx.RequestAborted) == false)
                return Results.NotFound();

            return Results.Created($"/api/cyberpanel/quizzes/{id}/attempts/00000000-0000-0000-0000-000000000000", new { Message = "Attempt scaffolded - implement logic" });
        }).RequireAuthorization();

        return group;
    }
}
