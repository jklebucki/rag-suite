using RAG.AddressBook.Endpoints;
using RAG.AddressBook.Extensions;
using RAG.CyberPanel.Endpoints;
using RAG.CyberPanel.Extensions;
using RAG.Forum.Endpoints;
using RAG.Forum.Extensions;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Extensions;
using RAG.Orchestrator.Api.Features.Analytics;
using RAG.Orchestrator.Api.Features.Chat;
using RAG.Orchestrator.Api.Features.Feedback;
using RAG.Orchestrator.Api.Features.FileDownload;
using RAG.Orchestrator.Api.Features.Health;
using RAG.Orchestrator.Api.Features.Plugins;
using RAG.Orchestrator.Api.Features.Search;
using RAG.Orchestrator.Api.Features.Settings;
using RAG.Orchestrator.Api.Services;
using RAG.Security.Extensions;
using RAG.Security.Middleware;
using Serilog;
using Serilog.Events;

// Configure Serilog so key backend operations are persisted to a rolling log file
// (the default console-only provider is invisible when the API runs as a service/container).
// File logging is configurable via the "FileLogging" section in appsettings.
var logConfiguration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

var fileLogSection = logConfiguration.GetSection("FileLogging");
var logPath = fileLogSection["Path"] ?? "logs/rag-orchestrator-.txt";
var logRollingInterval = Enum.TryParse<RollingInterval>(fileLogSection["RollingInterval"], ignoreCase: true, out var parsedInterval)
    ? parsedInterval
    : RollingInterval.Day; // roll daily by default
var logRetainedFileCountLimit = fileLogSection.GetValue<int?>("RetainedFileCountLimit") ?? 7; // keep 7 days by default

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
    // Feature areas we want to trace end-to-end (search/rerank/chat) at Information.
    .MinimumLevel.Override("RAG.Orchestrator.Api.Features.Search", LogEventLevel.Information)
    .MinimumLevel.Override("RAG.Orchestrator.Api.Features.Chat", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(logPath,
        rollingInterval: logRollingInterval,
        retainedFileCountLimit: logRetainedFileCountLimit,
        shared: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
Log.Information("Starting RAG Orchestrator API");

var builder = WebApplication.CreateBuilder(args);

// Route all ILogger<T> output through Serilog (console + rolling file).
builder.Host.UseSerilog();

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
builder.Services.AddFeatureServices(builder.Configuration);
// Register CyberPanel feature (scaffold)
builder.Services.AddCyberPanel(builder.Configuration);
// Register AddressBook feature
builder.Services.AddAddressBook(builder.Configuration);
// Register Forum feature
builder.Services.AddForum(builder.Configuration);

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

        // Ensure AddressBook database migrations are applied
        await app.Services.EnsureAddressBookDatabaseCreatedAsync();
        app.Logger.LogInformation("AddressBook database initialization completed successfully");

        // Ensure Forum database migrations are applied
        await app.Services.EnsureForumDatabaseCreatedAsync();
        app.Logger.LogInformation("Forum database initialization completed successfully");
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
    await globalSettingsService.InitializeForumSettingsAsync(configuration, chatDbContext);

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
app.MapFeedbackEndpoints();

app.MapCyberPanelEndpoints();

app.MapAddressBookEndpoints();
app.MapForumEndpoints();

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "RAG Orchestrator API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
