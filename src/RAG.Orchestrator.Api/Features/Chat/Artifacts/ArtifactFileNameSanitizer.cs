using RAG.DocumentProcessing.Abstractions;
using System.Text;

namespace RAG.Orchestrator.Api.Features.Chat.Artifacts;

public static class ArtifactFileNameSanitizer
{
    public static string Sanitize(string fileName, GeneratedArtifactFormat format)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(['/', '\\']) >= 0 || fileName.Contains("..", StringComparison.Ordinal))
        {
            throw new DocumentProcessingException("INVALID_ARTIFACT_FILE_NAME", "The generated artifact file name is invalid.");
        }

        var name = Path.GetFileNameWithoutExtension(fileName.Trim());
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(name.Length);
        foreach (var character in name)
        {
            sanitized.Append(invalid.Contains(character) || char.IsControl(character) ? '-' : character);
        }

        var baseName = sanitized.ToString().Trim(' ', '.', '-');
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "artifact";
        }

        baseName = baseName[..Math.Min(baseName.Length, 100)];
        var extension = format == GeneratedArtifactFormat.Docx ? ".docx" : ".txt";
        return baseName + extension;
    }
}
