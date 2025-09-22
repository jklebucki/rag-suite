using Microsoft.EntityFrameworkCore;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Extensions;
using RAG.Orchestrator.Api.Features.Analytics;
using RAG.Orchestrator.Api.Features.Chat;
using RAG.Orchestrator.Api.Features.FileDownload;
using RAG.Orchestrator.Api.Features.Health;
using RAG.Orchestrator.Api.Features.Plugins;
using RAG.Orchestrator.Api.Features.Search;
using RAG.Orchestrator.Api.Features.Settings;
using RAG.Orchestrator.Api.Services;
using RAG.Security.Extensions;
using RAG.Security.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRAGSecurity(builder.Configuration);
builder.Services.AddChatDatabase(builder.Configuration);
builder.Services.AddSemanticKernel();
builder.Services.AddLocalization(builder.Configuration);
builder.Services.AddSwaggerDocumentation();
builder.Services.AddCorsPolicy();
builder.Services.AddControllers();

// Configure FileDownload options
builder.Services.Configure<SharedFoldersOptions>(builder.Configuration.GetSection("Services"));

// Add feature services
builder.Services.AddFeatureServices();

var app = builder.Build();

// Ensure database is created and admin user exists
try
{
    await app.Services.EnsureSecurityDatabaseCreatedAsync();
    app.Logger.LogInformation("Security database initialization completed successfully");

    // Initialize Chat database and Elasticsearch
    await app.Services.EnsureChatDatabaseCreatedAsync();
    app.Logger.LogInformation("Chat database initialization completed successfully");

    // Initialize global settings from appsettings if not exist
    using var scope = app.Services.CreateScope();
    var globalSettingsService = scope.ServiceProvider.GetRequiredService<IGlobalSettingsService>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    await globalSettingsService.InitializeLlmSettingsAsync(configuration);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to initialize databases. Application will continue but some features may not work properly.");
    // Don't throw - let the application continue to run
}

app.UseSwagger();
app.UseSwaggerUI();

// Enable CORS for all environments (needed for direct API access)
app.UseCors("AllowFrontend");

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseUserContext();
app.UseAuthorization();

app.MapControllers();

// Map feature endpoints
//app.MapChatEndpoints();
app.MapUserChatEndpoints();
app.MapSearchEndpoints();
app.MapHealthEndpoints();
app.MapPluginEndpoints();
app.MapAnalyticsEndpoints();
app.MapFileDownloadEndpoints();
app.MapSettingsEndpoints();

// Simple health endpoint
app.MapGet("/health", (HttpContext context) =>
{
    try
    {
        return Results.Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "2.0.0-semantic-kernel"
        });
    }
    catch
    {
        return Results.Json(new
        {
            Status = "Error",
            Timestamp = DateTime.UtcNow,
            Version = "2.0.0-semantic-kernel"
        }, statusCode: 503);
    }
});

app.Run();
