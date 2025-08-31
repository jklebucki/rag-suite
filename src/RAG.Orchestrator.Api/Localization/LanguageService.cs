using System.Text.RegularExpressions;

namespace RAG.Orchestrator.Api.Localization;

/// <summary>
/// Implementation of language detection and translation services
/// </summary>
public class LanguageService : ILanguageService
{
    private readonly ILocalizedResources _localizedResources;
    private readonly LanguageConfiguration _configuration;
    private readonly ILogger<LanguageService> _logger;
    
    // Language detection patterns for basic heuristic detection
    private readonly Dictionary<string, List<string>> _languagePatterns = new()
    {
        ["pl"] = new() { "czy", "jak", "gdzie", "kiedy", "dlaczego", "że", "nie", "tak", "jest", "będzie", "może", "można" },
        ["en"] = new() { "the", "and", "what", "where", "when", "why", "how", "can", "will", "would", "should", "could" },
        ["ro"] = new() { "ce", "cum", "unde", "când", "de", "în", "cu", "pe", "la", "pentru", "este", "sunt" },
        ["hu"] = new() { "mi", "hogy", "hol", "mikor", "miért", "van", "lesz", "lehet", "kell", "fog", "szeretne" },
        ["nl"] = new() { "wat", "hoe", "waar", "wanneer", "waarom", "is", "zijn", "kan", "zal", "zou", "moet", "wil" }
    };
    
    public LanguageService(
        ILocalizedResources localizedResources,
        LanguageConfiguration configuration,
        ILogger<LanguageService> logger)
    {
        _localizedResources = localizedResources;
        _configuration = configuration;
        _logger = logger;
    }
    
    public string DetectLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return _configuration.DefaultLanguage;
        }
        
        // Convert to lowercase and split into words
        var words = Regex.Split(text.ToLower(), @"\W+")
            .Where(w => w.Length > 2)
            .Take(50) // Analyze first 50 meaningful words
            .ToList();
        
        if (!words.Any())
        {
            return _configuration.DefaultLanguage;
        }
        
        // Score each language based on pattern matches
        var languageScores = new Dictionary<string, int>();
        
        foreach (var (language, patterns) in _languagePatterns)
        {
            if (!_configuration.SupportedLanguages.Contains(language))
                continue;
                
            var score = words.Count(word => patterns.Contains(word));
            languageScores[language] = score;
        }
        
        // Return language with highest score, or default if no clear winner
        var detectedLanguage = languageScores
            .Where(kvp => kvp.Value > 0)
            .OrderByDescending(kvp => kvp.Value)
            .FirstOrDefault();
        
        var result = detectedLanguage.Key ?? _configuration.DefaultLanguage;
        
        _logger.LogDebug("Detected language: {Language} for text: {TextPreview}", 
            result, text.Length > 50 ? text[..50] + "..." : text);
        
        return result;
    }
    
    public async Task<TranslationResult> TranslateAsync(string text, string targetLanguage, string? sourceLanguage = null)
    {
        // For now, return a placeholder implementation
        // In a real implementation, this would integrate with Azure Translator Service
        
        if (string.IsNullOrWhiteSpace(text))
        {
            return new TranslationResult(string.Empty, sourceLanguage ?? "unknown", targetLanguage, 0.0, "placeholder", false);
        }
        
        var detectedSource = sourceLanguage ?? DetectLanguage(text);
        
        // If source and target are the same, return original text
        if (detectedSource == targetLanguage)
        {
            return new TranslationResult(text, detectedSource, targetLanguage, 1.0, "none", false);
        }
        
        // Placeholder translation logic
        // TODO: Implement Azure Translator Service integration
        _logger.LogInformation("Translation requested from {Source} to {Target}: {Text}", 
            detectedSource, targetLanguage, text.Length > 100 ? text[..100] + "..." : text);
        
        // For demo purposes, return the original text with a note
        var translatedText = $"[Translation from {detectedSource} to {targetLanguage}]: {text}";
        
        await Task.CompletedTask; // Placeholder for async operation
        
        return new TranslationResult(translatedText, detectedSource, targetLanguage, 0.8, "placeholder", false);
    }
    
    public async Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken = default)
    {
        var result = await TranslateAsync(text, targetLanguage, sourceLanguage);
        return result.TranslatedText;
    }
    
    public async Task<TranslationResult> TranslateWithConfidenceAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken = default)
    {
        return await TranslateAsync(text, targetLanguage, sourceLanguage);
    }
    
    public string GetDefaultLanguage()
    {
        return _configuration.DefaultLanguage;
    }
    
    public string[] GetSupportedLanguages()
    {
        return _configuration.SupportedLanguages.ToArray();
    }
    
    public string GetLocalizedString(string category, string key, string language)
    {
        return _localizedResources.GetString(category, key, language);
    }
    
    public string GetLocalizedErrorMessage(string errorKey, string language, params object[] args)
    {
        return _localizedResources.GetErrorMessage(errorKey, language, args);
    }
    
    public string GetLocalizedSystemPrompt(string promptKey, string language)
    {
        return _localizedResources.GetSystemPrompt(promptKey, language);
    }
    
    public string GetLocalizedInstructions(string language)
    {
        return _localizedResources.GetInstructions(language);
    }
    
    public bool IsLanguageSupported(string language)
    {
        return _configuration.SupportedLanguages.Contains(language);
    }
    
    public string NormalizeLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return _configuration.DefaultLanguage;
        }
        
        var normalized = language.ToLower().Trim();
        
        // Handle common language code variations
        var languageMap = new Dictionary<string, string>
        {
            ["polish"] = "pl",
            ["english"] = "en",
            ["romanian"] = "ro",
            ["hungarian"] = "hu",
            ["dutch"] = "nl",
            ["polski"] = "pl",
            ["angielski"] = "en",
            ["română"] = "ro",
            ["magyar"] = "hu",
            ["nederlands"] = "nl"
        };
        
        if (languageMap.TryGetValue(normalized, out var mappedLanguage))
        {
            normalized = mappedLanguage;
        }
        
        return IsLanguageSupported(normalized) ? normalized : _configuration.DefaultLanguage;
    }
}
