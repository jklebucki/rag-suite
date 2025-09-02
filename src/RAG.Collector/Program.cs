using RAG.Collector.Config;
using RAG.Collector.Workers;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/rag-collector-.txt", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true)
    .CreateLogger();

try
{
    Log.Information("Starting RAG Collector service");

    var builder = Host.CreateApplicationBuilder(args);

    // Add Serilog
    builder.Services.AddSerilog();

    // Configure CollectorOptions
    builder.Services.Configure<CollectorOptions>(
        builder.Configuration.GetSection(CollectorOptions.SectionName));

    // Add Windows Service support
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "RAG Collector";
    });

    // Register the main worker
    builder.Services.AddHostedService<CollectorWorker>();

    var host = builder.Build();

    // Log configuration details (mask sensitive data)
    var options = builder.Configuration.GetSection(CollectorOptions.SectionName).Get<CollectorOptions>();
    if (options != null)
    {
        Log.Information("Configuration loaded:");
        Log.Information("  Source folders: {@Folders}", options.SourceFolders);
        Log.Information("  File extensions: {@Extensions}", options.FileExtensions);
        Log.Information("  Chunk size: {ChunkSize}, overlap: {ChunkOverlap}", options.ChunkSize, options.ChunkOverlap);
        Log.Information("  Elasticsearch URL: {Url}", options.ElasticsearchUrl);
        Log.Information("  Index name: {IndexName}", options.IndexName);
        Log.Information("  Username configured: {HasUsername}", !string.IsNullOrEmpty(options.ElasticsearchUsername));
        Log.Information("  API key configured: {HasApiKey}", !string.IsNullOrEmpty(options.ElasticsearchApiKey));
        Log.Information("  Processing interval: {Interval} minutes", options.ProcessingIntervalMinutes);
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
