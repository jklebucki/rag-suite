using RAG.Orchestrator.Api.Localization;

namespace RAG.Orchestrator.Api.Extensions;

/// <summary>
/// Extension methods for registering localization services
/// </summary>
public static class LocalizationExtensions
{
    /// <summary>
    /// Registers localization services in the dependency injection container
    /// </summary>
    public static IServiceCollection AddLocalization(this IServiceCollection services, IConfiguration configuration)
    {
        // Register language configuration
        var languageConfig = new LanguageConfiguration();
        configuration.GetSection("Language").Bind(languageConfig);
        services.AddSingleton(languageConfig);

        // Register core localization services
        services.AddSingleton<ILocalizedResources, LocalizedResources>();
        services.AddSingleton<ILanguageService, LanguageService>();

        return services;
    }
}
