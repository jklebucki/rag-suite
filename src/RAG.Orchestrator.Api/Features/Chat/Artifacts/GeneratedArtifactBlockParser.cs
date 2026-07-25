using RAG.DocumentProcessing.Abstractions;
using System.Text.RegularExpressions;

namespace RAG.Orchestrator.Api.Features.Chat.Artifacts;

public static class GeneratedArtifactBlockParser
{
    private static readonly Regex ArtifactBlock = new(
        "<generated_artifact\\s+format=\"(?<format>[^\"]+)\"\\s+filename=\"(?<filename>[^\"]+)\">\\s*(?<content>.*?)\\s*</generated_artifact>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static GeneratedArtifactBlock? Extract(string response, out string cleanResponse)
    {
        var match = ArtifactBlock.Match(response);
        if (!match.Success)
        {
            cleanResponse = response;
            return null;
        }

        cleanResponse = ArtifactBlock.Replace(response, string.Empty).Trim();
        var format = match.Groups["format"].Value.Trim().ToLowerInvariant() switch
        {
            "txt" => GeneratedArtifactFormat.Txt,
            "docx" => GeneratedArtifactFormat.Docx,
            _ => throw new DocumentProcessingException("INVALID_ARTIFACT_FORMAT", "The generated artifact format is invalid.")
        };

        return new GeneratedArtifactBlock(
            format,
            match.Groups["filename"].Value,
            match.Groups["content"].Value.Trim());
    }
}
