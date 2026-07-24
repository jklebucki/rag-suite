using System.ComponentModel.DataAnnotations;

namespace RAG.Orchestrator.Api.Features.Chat.Artifacts;

public sealed class GeneratedArtifactOptions
{
    public const string SectionName = "Chat:GeneratedArtifacts";

    [Required]
    public string RootDirectory { get; init; } = "App_Data/generated-artifacts";

    [Range(1, 168)]
    public int ExpirationHours { get; init; } = 12;
}
