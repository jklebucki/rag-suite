using System.Text.Json;

namespace RAG.Orchestrator.Api.Localization;

/// <summary>
/// Implementation of localized resources using JSON files
/// </summary>
public class LocalizedResources : ILocalizedResources
{
    private readonly Dictionary<string, Dictionary<string, JsonElement>> _resources;
    private readonly ILogger<LocalizedResources> _logger;
    
    public LocalizedResources(ILogger<LocalizedResources> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _resources = new Dictionary<string, Dictionary<string, JsonElement>>();
        LoadResources(environment.ContentRootPath);
    }
    
    /// <summary>
    /// Load localization resources from JSON files
    /// </summary>
    private void LoadResources(string contentRootPath)
    {
        var localizationPath = Path.Combine(contentRootPath, "Localization");
        
        if (!Directory.Exists(localizationPath))
        {
            _logger.LogWarning("Localization directory not found: {Path}", localizationPath);
            return;
        }
        
        var jsonFiles = Directory.GetFiles(localizationPath, "*.json");
        
        foreach (var file in jsonFiles)
        {
            try
            {
                var language = Path.GetFileNameWithoutExtension(file);
                var jsonContent = File.ReadAllText(file);
                var jsonDocument = JsonDocument.Parse(jsonContent);
                
                _resources[language] = FlattenJsonDocument(jsonDocument.RootElement);
                
                _logger.LogInformation("Loaded localization resources for language: {Language}", language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load localization file: {File}", file);
            }
        }
    }
    
    /// <summary>
    /// Flatten nested JSON structure for easier access
    /// </summary>
    private Dictionary<string, JsonElement> FlattenJsonDocument(JsonElement element, string prefix = "")
    {
        var flattened = new Dictionary<string, JsonElement>();
        
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    var nested = FlattenJsonDocument(property.Value, key);
                    foreach (var nestedPair in nested)
                    {
                        flattened[nestedPair.Key] = nestedPair.Value;
                    }
                }
                else
                {
                    flattened[key] = property.Value;
                }
            }
        }
        
        return flattened;
    }
    
    public string GetString(string category, string key, string language)
    {
        var fullKey = $"{category}.{key}";
        
        if (_resources.TryGetValue(language, out var languageResources) &&
            languageResources.TryGetValue(fullKey, out var value))
        {
            return value.GetString() ?? string.Empty;
        }
        
        // Fallback to English
        if (language != "en" && _resources.TryGetValue("en", out var englishResources) &&
            englishResources.TryGetValue(fullKey, out var englishValue))
        {
            _logger.LogWarning("Missing localization for {Language}: {Key}, using English fallback", language, fullKey);
            return englishValue.GetString() ?? string.Empty;
        }
        
        _logger.LogWarning("Missing localization for key: {Key} in language: {Language}", fullKey, language);
        return $"[{fullKey}]";
    }
    
    public string GetErrorMessage(string errorKey, string language, params object[] args)
    {
        var template = GetString("error_messages", errorKey, language);
        return FormatString(template, args);
    }
    
    public string GetSystemPrompt(string promptKey, string language)
    {
        return GetString("system_prompts", promptKey, language);
    }
    
    public string GetInstructions(string language)
    {
        var instructions = new[]
        {
            GetString("instructions", "respond_in_language", language),
            GetString("instructions", "use_knowledge_base", language),
            GetString("instructions", "consider_history", language),
            GetString("instructions", "be_honest", language),
            GetString("instructions", "be_helpful", language)
        };
        
        return string.Join("\n- ", new[] { "" }.Concat(instructions));
    }
    
    public string FormatString(string template, params object[] args)
    {
        try
        {
            return args.Length > 0 ? string.Format(template, args) : template;
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Failed to format string template: {Template}", template);
            return template;
        }
    }
}
