using RAG.Orchestrator.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddSemanticKernel();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddCorsPolicy();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RAG Orchestrator API v1");
        options.RoutePrefix = string.Empty;
    });
    app.UseCors("AllowFrontend");
}

app.MapControllers();

// Simple health endpoint
app.MapGet("/health", () => new { 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow,
    Version = "2.0.0-semantic-kernel-demo"
});

app.Run();
