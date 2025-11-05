namespace RAG.Orchestrator.Api.Common.Constants;

/// <summary>
/// Constants for supported language codes
/// </summary>
public static class SupportedLanguages
{
    /// <summary>
    /// English language code
    /// </summary>
    public const string English = "en";

    /// <summary>
    /// Polish language code
    /// </summary>
    public const string Polish = "pl";

    /// <summary>
    /// Hungarian language code
    /// </summary>
    public const string Hungarian = "hu";

    /// <summary>
    /// Dutch language code
    /// </summary>
    public const string Dutch = "nl";

    /// <summary>
    /// Romanian language code
    /// </summary>
    public const string Romanian = "ro";

    /// <summary>
    /// Default language code
    /// </summary>
    public const string Default = English;

    /// <summary>
    /// Array of all supported language codes
    /// </summary>
    public static readonly string[] All = { English, Polish, Hungarian, Dutch, Romanian };
}

