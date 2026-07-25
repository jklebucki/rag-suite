using RAG.DocumentProcessing.Api;
using RAG.DocumentProcessing.Core;
using RAG.DocumentProcessing.Providers.Docling;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDoclingDocumentProcessingProvider(builder.Configuration);
builder.Services.AddDocumentProcessingCore(builder.Configuration);

var app = builder.Build();
var configuredApiKey = builder.Configuration["DocumentProcessing:ApiKey"];
app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/health") || string.IsNullOrWhiteSpace(configuredApiKey))
    {
        await next(context);
        return;
    }

    var providedApiKey = context.Request.Headers["X-Api-Key"].ToString();
    var matches = CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(providedApiKey),
        Encoding.UTF8.GetBytes(configuredApiKey));
    if (!matches)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    await next(context);
});

app.MapDocumentProcessingEndpoints();
app.Run();

public partial class Program
{
}
