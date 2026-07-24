namespace RAG.DocumentProcessing.Abstractions;

public interface ITemporaryArtifactStore
{
    Task<StoredArtifact> SaveAsync(
        GeneratedArtifact artifact,
        CancellationToken cancellationToken);

    Task<StoredArtifact?> GetAsync(
        string artifactId,
        string userId,
        CancellationToken cancellationToken);

    Task DeleteExpiredAsync(CancellationToken cancellationToken);
}
