using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;

namespace RAG.Shared;

public static class PathHelper
{
    /// <summary>
    /// Gets the project root directory in a cross-platform way
    /// </summary>
    public static string GetProjectRoot()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var directory = new DirectoryInfo(currentDirectory);
        
        // Look for project root indicators
        while (directory != null && !IsProjectRoot(directory))
        {
            directory = directory.Parent;
        }
        
        return directory?.FullName ?? currentDirectory;
    }
    
    /// <summary>
    /// Gets the documents directory path relative to project root
    /// </summary>
    public static string GetDocumentsPath(IConfiguration? configuration = null)
    {
        // Try to get from configuration first
        var configPath = configuration?["Ingestion:DocumentsPath"];
        if (!string.IsNullOrEmpty(configPath))
        {
            // If it's a relative path, make it relative to project root
            if (!Path.IsPathRooted(configPath))
            {
                return Path.Combine(GetProjectRoot(), configPath);
            }
            return configPath;
        }
        
        // Default fallback
        return Path.Combine(GetProjectRoot(), "data", "documents");
    }
    
    /// <summary>
    /// Normalizes a path to use the correct directory separator for the current OS
    /// </summary>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;
            
        // Replace any separator with the correct one for current OS
        return path.Replace('\\', Path.DirectorySeparatorChar)
                  .Replace('/', Path.DirectorySeparatorChar);
    }
    
    /// <summary>
    /// Combines paths in a cross-platform way
    /// </summary>
    public static string CombinePaths(params string[] paths)
    {
        if (paths == null || paths.Length == 0)
            return string.Empty;
            
        var combined = paths[0];
        for (int i = 1; i < paths.Length; i++)
        {
            combined = Path.Combine(combined, paths[i]);
        }
        
        return NormalizePath(combined);
    }
    
    /// <summary>
    /// Checks if a directory is likely the project root
    /// </summary>
    private static bool IsProjectRoot(DirectoryInfo directory)
    {
        // Look for common project root indicators
        var indicators = new[]
        {
            "RAGSuite.sln",           // Solution file
            ".git",                   // Git repository
            "src",                    // Source directory
            "data",                   // Data directory
            "scripts",                // Scripts directory
            "deploy"                  // Deploy directory
        };
        
        return indicators.Any(indicator => 
            File.Exists(Path.Combine(directory.FullName, indicator)) ||
            Directory.Exists(Path.Combine(directory.FullName, indicator)));
    }
    
    /// <summary>
    /// Creates a directory if it doesn't exist
    /// </summary>
    public static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
    
    /// <summary>
    /// Gets the appropriate temp directory for the current OS
    /// </summary>
    public static string GetTempDirectory()
    {
        return Path.GetTempPath();
    }
    
    /// <summary>
    /// Checks if running on Windows
    /// </summary>
    public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    
    /// <summary>
    /// Checks if running on Linux
    /// </summary>
    public static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    
    /// <summary>
    /// Checks if running on macOS
    /// </summary>
    public static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    
    /// <summary>
    /// Gets the current OS name for logging/debugging
    /// </summary>
    public static string GetOSName()
    {
        if (IsWindows()) return "Windows";
        if (IsLinux()) return "Linux";
        if (IsMacOS()) return "macOS";
        return "Unknown";
    }
}
