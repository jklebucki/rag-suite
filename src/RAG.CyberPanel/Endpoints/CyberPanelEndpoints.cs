using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RAG.CyberPanel.Features.CreateQuiz;
using RAG.CyberPanel.Features.DeleteAttempt;
using RAG.CyberPanel.Features.DeleteQuiz;
using RAG.CyberPanel.Features.ExportQuiz;
using RAG.CyberPanel.Features.GetAttemptById;
using RAG.CyberPanel.Features.GetQuiz;
using RAG.CyberPanel.Features.ImportQuiz;
using RAG.CyberPanel.Features.ListAttempts;
using RAG.CyberPanel.Features.ListQuizzes;
using RAG.CyberPanel.Features.SubmitAttempt;
using RAG.CyberPanel.Features.UpdateQuiz;

namespace RAG.CyberPanel.Endpoints;

/// <summary>
/// Maps all CyberPanel endpoints using Vertical Slice Architecture.
/// Each feature (CreateQuiz, UpdateQuiz, DeleteQuiz, GetQuiz, ListQuizzes, SubmitAttempt, ListAttempts, GetAttemptById) owns its endpoint, service, models, and validators.
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
        group.MapUpdateQuiz();
        group.MapDeleteQuiz();
        group.MapSubmitAttempt();
        group.MapListAttempts();
        group.MapGetAttemptById();
        group.MapDeleteAttempt();
        group.MapExportQuiz();
        group.MapImportQuiz();

        return group;
    }
}