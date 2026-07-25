using RAG.DocumentProcessing.Abstractions;

namespace RAG.Orchestrator.Api.Features.Chat.Artifacts;

public sealed class GeneratedArtifactService : IGeneratedArtifactService
{
    private readonly IArtifactContentRenderer _renderer;
    private readonly ITemporaryArtifactStore _artifactStore;
    private readonly ILogger<GeneratedArtifactService> _logger;

    public GeneratedArtifactService(
        IArtifactContentRenderer renderer,
        ITemporaryArtifactStore artifactStore,
        ILogger<GeneratedArtifactService> logger)
    {
        _renderer = renderer;
        _artifactStore = artifactStore;
        _logger = logger;
    }

    public async Task<ArtifactGenerationResult> ProcessAsync(
        string response,
        string userId,
        string sessionId,
        string assistantMessageId,
        CancellationToken cancellationToken)
    {
        GeneratedArtifactBlock? block;
        string cleanResponse;
        try
        {
            block = GeneratedArtifactBlockParser.Extract(response, out cleanResponse);
        }
        catch (DocumentProcessingException ex)
        {
            _logger.LogWarning("Discarded invalid generated artifact block: {Code}", ex.Code);
            return new ArtifactGenerationResult(AppendFailureNotice(RemoveArtifactBlocks(response)), false);
        }

        if (block == null)
        {
            return new ArtifactGenerationResult(response, false);
        }

        var artifactResult = await CreateAsync(
            block.Format,
            block.FileName,
            block.Markdown,
            userId,
            sessionId,
            assistantMessageId,
            cancellationToken);
        return artifactResult.ArtifactCreated
            ? artifactResult with { Response = $"{cleanResponse}\n\n{artifactResult.Response}".Trim() }
            : artifactResult with { Response = AppendFailureNotice(cleanResponse) };
    }

    public async Task<ArtifactGenerationResult> CreateAsync(
        GeneratedArtifactFormat format,
        string fileName,
        string markdown,
        string userId,
        string sessionId,
        string assistantMessageId,
        CancellationToken cancellationToken)
    {
        try
        {
            var sanitizedFileName = ArtifactFileNameSanitizer.Sanitize(fileName, format);
            var content = _renderer.Render(format, markdown);
            var artifact = new GeneratedArtifact(
                format,
                sanitizedFileName,
                _renderer.GetContentType(format),
                content,
                userId,
                sessionId,
                assistantMessageId);
            var stored = await _artifactStore.SaveAsync(artifact, cancellationToken);
            var expiry = ToWarsaw(stored.ExpiresAt);
            var link = $"[Pobierz {stored.FileName}](/api/user-chat/artifacts/{stored.Id}/download)";
            var expiresAt = $"_Link ważny do: {expiry:yyyy-MM-dd HH:mm} Europe/Warsaw._";
            return new ArtifactGenerationResult($"{link}\n\n{expiresAt}", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save generated artifact for user {UserId}", userId);
            return new ArtifactGenerationResult(string.Empty, false);
        }
    }

    private static string AppendFailureNotice(string response)
    {
        return $"{response.Trim()}\n\n_The requested file could not be prepared; the text response is still available._".Trim();
    }

    private static string RemoveArtifactBlocks(string response)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            response,
            @"<generated_artifact\b.*?</generated_artifact>",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline).Trim();
    }

    private static DateTimeOffset ToWarsaw(DateTimeOffset timestamp)
    {
        try
        {
            return TimeZoneInfo.ConvertTime(timestamp, TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw"));
        }
        catch (TimeZoneNotFoundException)
        {
            return timestamp;
        }
    }
}
