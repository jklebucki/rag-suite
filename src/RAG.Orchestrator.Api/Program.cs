using RAG.Orchestrator.Api.Extensions;
using RAG.Orchestrator.Api.Features.Chat;
using RAG.Orchestrator.Api.Features.Search;
using RAG.Orchestrator.Api.Features.Health;
using RAG.Orchestrator.Api.Features.Plugins;
using RAG.Orchestrator.Api.Features.Analytics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddSemanticKernel();
builder.Services.AddLocalization(builder.Configuration);
builder.Services.AddSwaggerDocumentation();
builder.Services.AddCorsPolicy();
builder.Services.AddControllers();

// Add feature services
builder.Services.AddFeatureServices();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowFrontend");
}

app.MapControllers();

// Map feature endpoints
app.MapChatEndpoints();
app.MapSearchEndpoints();
app.MapHealthEndpoints();
app.MapPluginEndpoints();
app.MapAnalyticsEndpoints();

// Simple health endpoint
app.MapGet("/health", () => new { 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow,
    Version = "2.0.0-semantic-kernel-demo"
});

app.Run();
