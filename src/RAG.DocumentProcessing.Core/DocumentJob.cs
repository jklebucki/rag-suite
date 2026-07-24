using RAG.DocumentProcessing.Abstractions;

namespace RAG.DocumentProcessing.Core;

internal sealed class DocumentJob
{
    private readonly object _sync = new();

    public DocumentJob(string id, DocumentDescriptor document, string inputPath, int pageCount)
    {
        Id = id;
        Document = document;
        InputPath = inputPath;
        PageCount = pageCount;
        AcceptedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; }

    public DocumentDescriptor Document { get; }

    public string InputPath { get; }

    public int PageCount { get; }

    public DateTimeOffset AcceptedAt { get; }

    public CancellationTokenSource Cancellation { get; } = new();

    public DocumentJobState State { get; private set; } = DocumentJobState.Queued;

    public int Progress { get; private set; }

    public string? Provider { get; private set; }

    public string? ErrorCode { get; private set; }

    public DocumentProcessingResult? Result { get; private set; }

    public void MarkProcessing()
    {
        lock (_sync)
        {
            State = DocumentJobState.Processing;
            Progress = 10;
        }
    }

    public void MarkReady(DocumentProcessingResult result)
    {
        lock (_sync)
        {
            Result = result;
            Provider = result.Provider;
            State = DocumentJobState.Ready;
            Progress = 100;
        }
    }

    public void MarkFailed(string errorCode)
    {
        lock (_sync)
        {
            ErrorCode = errorCode;
            State = DocumentJobState.Failed;
            Progress = 100;
        }
    }

    public DocumentJobStatus GetStatus()
    {
        lock (_sync)
        {
            return new DocumentJobStatus(Id, State, Progress, PageCount, Provider, ErrorCode);
        }
    }

    public DocumentProcessingResult? GetResult()
    {
        lock (_sync)
        {
            return Result;
        }
    }
}
