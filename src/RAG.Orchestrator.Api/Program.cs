using RAG.Orchestrator.Api.Extensions;
using RAG.Orchestrator.Api.Features.Analytics;
using RAG.Orchestrator.Api.Features.Chat;
using RAG.Orchestrator.Api.Features.Plugins;
using RAG.Orchestrator.Api.Features.Search;
using RAG.Orchestrator.Api.Features.Health;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for longer timeouts
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
});

// Add services to the container
builder.Services
    .AddApplicationServices()
    .AddSwaggerDocumentation()
    .AddCorsPolicy();

// Configure HTTPS redirection
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 7108; // Port HTTPS z launchSettings.json
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RAG Orchestrator API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });
    app.UseCors("AllowFrontend");
}
else
{
    // Użyj HTTPS redirection tylko w środowisku produkcyjnym
    app.UseHttpsRedirection();
}

// Map feature endpoints
app.MapSearchEndpoints();
app.MapChatEndpoints();
app.MapPluginEndpoints();
app.MapAnalyticsEndpoints();
app.MapHealthEndpoints();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithTags("Health")
   .WithSummary("Health check endpoint")
   .WithDescription("Returns the health status of the API");

app.Run();
