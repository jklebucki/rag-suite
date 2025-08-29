using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using RAG.Connectors.Files;
using RAG.Ingestion.Worker;
using RAG.Ingestion.Worker.Models;
using RAG.Ingestion.Worker.Services;
using RAG.Shared;

var builder = Host.CreateApplicationBuilder(args);

// Configure settings
var ingestionSettings = new IngestionSettings();
builder.Configuration.GetSection("Ingestion").Bind(ingestionSettings);

// Use PathHelper to resolve the documents path
if (!string.IsNullOrEmpty(ingestionSettings.DocumentsPath))
{
    ingestionSettings.DocumentsPath = PathHelper.GetDocumentsPath(builder.Configuration);
}

builder.Services.AddSingleton(ingestionSettings);

// Configure Elasticsearch with authentication
var elasticsearchSettings = new ElasticsearchClientSettings(new Uri(ingestionSettings.ElasticsearchUrl))
    .Authentication(new BasicAuthentication(ingestionSettings.ElasticsearchUsername, ingestionSettings.ElasticsearchPassword));

var elasticsearchClient = new ElasticsearchClient(elasticsearchSettings);
builder.Services.AddSingleton(elasticsearchClient);

// Register Files connector
builder.Services.AddFilesConnector();

// Register ingestion services
builder.Services.AddTransient<ITextChunkingService, TextChunkingService>();
builder.Services.AddTransient<IEmbeddingService, SimpleEmbeddingService>();
builder.Services.AddTransient<IElasticsearchService, ElasticsearchService>();
builder.Services.AddTransient<IDocumentIngestionService, DocumentIngestionService>();

// Register background worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
