namespace RAG.DocumentProcessing.Abstractions;

public enum DocumentJobState
{
    Queued,
    Processing,
    Ready,
    Failed
}
