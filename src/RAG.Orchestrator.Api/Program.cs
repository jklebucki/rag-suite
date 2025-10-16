using RAG.CyberPanel.Endpoints;
using RAG.CyberPanel.Extensions;
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
// Register CyberPanel feature (scaffold)
builder.Services.AddCyberPanel(builder.Configuration);

var app = builder.Build();

// Ensure database is created and admin user exists
try
{
    // Migration auto-apply gating (default: true)
    var autoApply = builder.Configuration.GetValue<bool>("Migrations:AutoApply", true);

    if (autoApply)
    {
        await app.Services.EnsureSecurityDatabaseCreatedAsync();
        app.Logger.LogInformation("Security database initialization completed successfully");

        // Initialize Chat database and Elasticsearch
        await app.Services.EnsureChatDatabaseCreatedAsync();
        app.Logger.LogInformation("Chat database initialization completed successfully");

        // Ensure CyberPanel database migrations are applied
        await app.Services.EnsureCyberPanelDatabaseCreatedAsync();
        app.Logger.LogInformation("CyberPanel database initialization completed successfully");
    }
    else
    {
        app.Logger.LogInformation("Automatic DB migrations disabled by configuration (Migrations:AutoApply=false). Skipping migrations and seeding.");
    }

    // Initialize global settings from appsettings if not exist
    using var scope = app.Services.CreateScope();
    var globalSettingsService = scope.ServiceProvider.GetRequiredService<IGlobalSettingsService>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var chatDbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    await globalSettingsService.InitializeLlmSettingsAsync(configuration, chatDbContext);

    // Initialize global settings cache
    var globalSettingsCache = scope.ServiceProvider.GetRequiredService<IGlobalSettingsCache>();
    await globalSettingsCache.InitializeAsync(chatDbContext);
    app.Logger.LogInformation("Global settings cache initialization completed successfully");
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

app.MapCyberPanelEndpoints();

app.Run();
