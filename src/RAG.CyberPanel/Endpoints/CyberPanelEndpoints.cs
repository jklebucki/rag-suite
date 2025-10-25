using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RAG.CyberPanel.Features.CreateQuiz;
using RAG.CyberPanel.Features.ExportQuiz;
using RAG.CyberPanel.Features.GetQuiz;
using RAG.CyberPanel.Features.ListQuizzes;
using RAG.CyberPanel.Features.SubmitAttempt;

namespace RAG.CyberPanel.Endpoints;

/// <summary>
/// Maps all CyberPanel endpoints using Vertical Slice Architecture.
/// Each feature (CreateQuiz, GetQuiz, ListQuizzes, SubmitAttempt) owns its endpoint, service, models, and validators.
/// </summary>
public static class CyberPanelEndpoints
{
    public static RouteGroupBuilder MapCyberPanelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/cyberpanel/quizzes")
            .WithTags("CyberPanel")
            .WithOpenApi();

        // Map feature endpoints following Vertical Slice Architecture
        group.MapListQuizzes();
        group.MapGetQuiz();
        group.MapCreateQuiz();
        group.MapSubmitAttempt();
        group.MapExportQuiz();

        return group;
    }
}
