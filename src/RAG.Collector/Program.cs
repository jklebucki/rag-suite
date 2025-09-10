using Elasticsearch.Net;
using Microsoft.Extensions.Options;
using RAG.Collector.Acl;
using RAG.Collector.Chunking;
using RAG.Collector.Config;
using RAG.Collector.ContentExtractors;
using RAG.Collector.Elasticsearch;
using RAG.Collector.Embeddings;
using RAG.Collector.Enumerators;
using RAG.Collector.Indexing;
using RAG.Collector.Workers;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("System.Net.Http.HttpClient", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/rag-collector-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting RAG Collector service");

    var builder = Host.CreateApplicationBuilder(args);

    // Add Serilog
    builder.Services.AddSerilog();

    // Configure CollectorOptions with validation
    builder.Services.Configure<CollectorOptions>(
        builder.Configuration.GetSection(CollectorOptions.SectionName));

    builder.Services.AddOptionsWithValidateOnStart<CollectorOptions>();

    // Add Windows Service support
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "RAG Collector";
    });

    // Register services
    builder.Services.AddScoped<IFileEnumerator, FileEnumerator>();
    builder.Services.AddScoped<IAclResolver, NtfsAclResolver>();

    // Register content extractors
    builder.Services.AddScoped<IContentExtractor, PlainTextExtractor>();
    builder.Services.AddScoped<IContentExtractor, PdfExtractor>();
    builder.Services.AddScoped<IContentExtractor, OfficeDocumentExtractor>();
    builder.Services.AddScoped<ContentExtractionService>();

    // Register chunking services
    builder.Services.AddScoped<ChunkingService>();

    // Register HTTP client for embedding service
    builder.Services.AddHttpClient("EmbeddingService");

    // Register configured HttpClient for HttpEmbeddingProvider
    builder.Services.AddScoped<HttpClient>(serviceProvider =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<CollectorOptions>>().Value;
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient("EmbeddingService");
        client.BaseAddress = new Uri(options.EmbeddingServiceUrl);
        client.Timeout = TimeSpan.FromMinutes(2);
        return client;
    });

    // Register Elasticsearch client
    builder.Services.AddSingleton<IElasticLowLevelClient>(serviceProvider =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<CollectorOptions>>().Value;
        var settings = new ConnectionConfiguration(new Uri(options.ElasticsearchUrl));

        if (!string.IsNullOrEmpty(options.ElasticsearchUsername) && !string.IsNullOrEmpty(options.ElasticsearchPassword))
        {
            settings = settings.BasicAuthentication(options.ElasticsearchUsername, options.ElasticsearchPassword);
        }

        if (options.AllowSelfSignedCert)
        {
            settings = settings.ServerCertificateValidationCallback((o, certificate, chain, errors) => true);
        }

        return new ElasticLowLevelClient(settings);
    });

    // Register embedding and indexing services
    builder.Services.AddScoped<IEmbeddingProvider, HttpEmbeddingProvider>();
    builder.Services.AddScoped<IElasticsearchService, ElasticsearchService>();
    builder.Services.AddScoped<IFileChangeDetectionService, FileChangeDetectionService>();
    builder.Services.AddScoped<IndexingService>();
    builder.Services.AddScoped<IOrphanedDocumentCleanupService, OrphanedDocumentCleanupService>();

    // Register the main worker
    builder.Services.AddHostedService<CollectorWorker>();

    var host = builder.Build();

    // Validate and log configuration details
    var options = builder.Configuration.GetSection(CollectorOptions.SectionName).Get<CollectorOptions>();
    if (options != null)
    {
        // Validate configuration
        var validationResults = options.Validate().ToList();
        if (validationResults.Any())
        {
            Log.Error("Configuration validation failed:");
            foreach (var result in validationResults)
            {
                Log.Error("  {ErrorMessage} (Properties: {Properties})",
                    result.ErrorMessage,
                    string.Join(", ", result.MemberNames));
            }
            throw new InvalidOperationException("Invalid configuration. See logs for details.");
        }

        Log.Information("Configuration validation successful");
        Log.Information("Configuration: {Config}", options.ToLogString());
    }
    else
    {
        Log.Error("Failed to load CollectorOptions from configuration");
        throw new InvalidOperationException("CollectorOptions configuration section is missing or invalid");
    }

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
