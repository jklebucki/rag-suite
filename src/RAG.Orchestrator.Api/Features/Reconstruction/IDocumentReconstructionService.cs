using RAG.Orchestrator.Api.Features.Search;

namespace RAG.Orchestrator.Api.Features.Reconstruction;

/// <summary>
/// Interface for document reconstruction services that handle overlap removal
/// </summary>
public interface IDocumentReconstructionService
{
    /// <summary>
    /// Reconstructs a complete document from chunks, removing overlaps between adjacent chunks
    /// </summary>
    /// <param name="chunks">List of chunks sorted by index</param>
    /// <param name="removeOverlap">Whether to remove overlap between chunks</param>
    /// <returns>Reconstructed document content without overlaps</returns>
    string ReconstructDocument(IEnumerable<ChunkInfo> chunks, bool removeOverlap = true);

    /// <summary>
    /// Removes overlap between two adjacent text segments
    /// </summary>
    /// <param name="previousText">The previous text segment</param>
    /// <param name="currentText">The current text segment</param>
    /// <param name="maxOverlapLength">Maximum expected overlap length</param>
    /// <returns>Current text with overlap removed</returns>
    string RemoveOverlap(string previousText, string currentText, int maxOverlapLength = 200);
}
