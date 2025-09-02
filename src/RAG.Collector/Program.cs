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

    // Configure CollectorOptions with validation
    builder.Services.Configure<CollectorOptions>(
        builder.Configuration.GetSection(CollectorOptions.SectionName));
    
    builder.Services.AddOptionsWithValidateOnStart<CollectorOptions>();

    // Add Windows Service support
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "RAG Collector";
    });

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
