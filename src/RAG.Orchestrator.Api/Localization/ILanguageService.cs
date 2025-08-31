namespace RAG.Orchestrator.Api.Localization;

/// <summary>
/// Service for language detection, translation, and localization
/// </summary>
public interface ILanguageService
{
    /// <summary>
    /// Detects the language of the provided text
    /// </summary>
    string DetectLanguage(string text);
    
    /// <summary>
    /// Gets the default language
    /// </summary>
    string GetDefaultLanguage();
    
    /// <summary>
    /// Gets all supported languages
    /// </summary>
    string[] GetSupportedLanguages();
    
    /// <summary>
    /// Checks if a language is supported
    /// </summary>
    bool IsLanguageSupported(string language);
    
    /// <summary>
    /// Normalizes language code to supported format
    /// </summary>
    string NormalizeLanguage(string language);
    
    /// <summary>
    /// Translates text asynchronously
    /// </summary>
    Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Translates text with confidence scoring
    /// </summary>
    Task<TranslationResult> TranslateWithConfidenceAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a localized string by category and key
    /// </summary>
    string GetLocalizedString(string category, string key, string language);
    
    /// <summary>
    /// Gets a localized error message
    /// </summary>
    string GetLocalizedErrorMessage(string errorKey, string language, params object[] args);
    
    /// <summary>
    /// Gets a localized system prompt
    /// </summary>
    string GetLocalizedSystemPrompt(string promptKey, string language);
    
    /// <summary>
    /// Gets localized instructions
    /// </summary>
    string GetLocalizedInstructions(string language);
}

/// <summary>
/// Result of text translation with metadata
/// </summary>
public record TranslationResult(
    string TranslatedText,
    string SourceLanguage,
    string TargetLanguage,
    double Confidence,
    string Provider,
    bool FromCache = false
);

/// <summary>
/// Interface for localized resources management
/// </summary>
public interface ILocalizedResources
{
    /// <summary>
    /// Gets a localized string by key and language
    /// </summary>
    string GetString(string category, string key, string language);
    
    /// <summary>
    /// Gets a localized error message
    /// </summary>
    string GetErrorMessage(string errorKey, string language, params object[] args);
    
    /// <summary>
    /// Gets a system prompt for the specified language
    /// </summary>
    string GetSystemPrompt(string promptKey, string language);
    
    /// <summary>
    /// Gets formatted instruction text for the specified language
    /// </summary>
    string GetInstructions(string language);
    
    /// <summary>
    /// Formats a localized string with parameters
    /// </summary>
    string FormatString(string template, params object[] args);
}

/// <summary>
/// Configuration for language support and settings
/// </summary>
public class LanguageConfiguration
{
    public string DefaultLanguage { get; set; } = "en";
    
    public List<string> SupportedLanguages { get; set; } = new()
    {
        "pl", "en", "ro", "hu", "nl"
    };
    
    public bool EnableTranslation { get; set; } = true;
    
    public bool EnableLanguageDetection { get; set; } = true;
    
    public int TranslationCacheExpirationMinutes { get; set; } = 60;
    
    public double MinimumConfidenceThreshold { get; set; } = 0.7;
    
    public string TranslationProvider { get; set; } = "Azure";
    
    /// <summary>
    /// Azure Translator Service configuration
    /// </summary>
    public AzureTranslatorConfig AzureTranslator { get; set; } = new();
}

/// <summary>
/// Azure Translator Service specific configuration
/// </summary>
public class AzureTranslatorConfig
{
    public string? SubscriptionKey { get; set; }
    
    public string? Region { get; set; }
    
    public string Endpoint { get; set; } = "https://api.cognitive.microsofttranslator.com";
    
    public string ApiVersion { get; set; } = "3.0";
    
    public bool UseKeyVault { get; set; } = false;
    
    public string? KeyVaultSecretName { get; set; }
}
