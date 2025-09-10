using Microsoft.EntityFrameworkCore;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Extensions;
using RAG.Orchestrator.Api.Features.Analytics;
using RAG.Orchestrator.Api.Features.Chat;
using RAG.Orchestrator.Api.Features.Health;
using RAG.Orchestrator.Api.Features.Plugins;
using RAG.Orchestrator.Api.Features.Search;
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

// Add feature services
builder.Services.AddFeatureServices();

var app = builder.Build();

// Ensure database is created and admin user exists
try
{
    await app.Services.EnsureSecurityDatabaseCreatedAsync();
    app.Logger.LogInformation("Security database initialization completed successfully");

    // Initialize Chat database
    using var scope = app.Services.CreateScope();
    var chatDbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    await chatDbContext.Database.MigrateAsync();
    app.Logger.LogInformation("Chat database initialization completed successfully");

    // Initialize Elasticsearch indices
    var indexManagement = scope.ServiceProvider.GetRequiredService<IIndexManagementService>();
    var elasticsearchOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ElasticsearchOptions>>().Value;

    if (elasticsearchOptions.AutoCreateIndices)
    {
        var indexCreated = await indexManagement.EnsureIndexExistsAsync(elasticsearchOptions.DefaultIndexName);
        if (indexCreated)
        {
            app.Logger.LogInformation("Elasticsearch index '{IndexName}' initialization completed successfully", elasticsearchOptions.DefaultIndexName);
        }
        else
        {
            app.Logger.LogWarning("Failed to initialize Elasticsearch index '{IndexName}'. Search functionality may not work properly", elasticsearchOptions.DefaultIndexName);
        }
    }
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
app.MapChatEndpoints();
app.MapUserChatEndpoints();
app.MapSearchEndpoints();
app.MapHealthEndpoints();
app.MapPluginEndpoints();
app.MapAnalyticsEndpoints();

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
