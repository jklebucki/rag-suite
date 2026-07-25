using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RAG.DocumentProcessing.Abstractions;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace RAG.DocumentProcessing.Core;

public sealed class DocumentProcessingJobService : IDocumentProcessingJobService
{
    private readonly ConcurrentDictionary<string, DocumentJob> _jobs = new(StringComparer.Ordinal);
    private readonly Channel<string> _queue;
    private readonly IDocumentProcessingProvider _provider;
    private readonly DocumentProcessingOptions _options;
    private readonly ILogger<DocumentProcessingJobService> _logger;
    private readonly string _workDirectory;

    public DocumentProcessingJobService(
        IDocumentProcessingProvider provider,
        IOptions<DocumentProcessingOptions> options,
        IWebHostEnvironment environment,
        ILogger<DocumentProcessingJobService> logger)
    {
        _provider = provider;
        _options = options.Value;
        _logger = logger;
        _queue = Channel.CreateBounded<string>(new BoundedChannelOptions(_options.MaxQueuedJobs)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = false
        });
        _workDirectory = Path.GetFullPath(Path.Combine(environment.ContentRootPath, _options.WorkDirectory));
        Directory.CreateDirectory(_workDirectory);
    }

    public async Task<DocumentJobAccepted> SubmitAsync(
        DocumentDescriptor document,
        Stream content,
        CancellationToken cancellationToken)
    {
        ValidateDocument(document);
        var id = Guid.NewGuid().ToString("N");
        var jobDirectory = Path.Combine(_workDirectory, id);
        var inputPath = Path.Combine(jobDirectory, "input.pdf");
        Directory.CreateDirectory(jobDirectory);

        try
        {
            await using (var destination = File.Create(inputPath))
            {
                await content.CopyToAsync(destination, cancellationToken);
            }

            if (!await PdfPageCounter.HasPdfHeaderAsync(inputPath, cancellationToken))
            {
                throw new DocumentProcessingException("INVALID_PDF", "The uploaded file is not a valid PDF.");
            }

            var pageCount = await PdfPageCounter.CountAsync(inputPath, cancellationToken);
            if (pageCount > _options.MaxPageCount)
            {
                throw new DocumentProcessingException("PAGE_LIMIT_EXCEEDED", $"The document exceeds the {_options.MaxPageCount}-page limit.");
            }

            var job = new DocumentJob(id, document, inputPath, pageCount);
            if (!_jobs.TryAdd(id, job) || !_queue.Writer.TryWrite(id))
            {
                _jobs.TryRemove(id, out _);
                throw new DocumentProcessingException("QUEUE_FULL", "The document processing queue is full. Try again later.");
            }

            return new DocumentJobAccepted(id, DocumentJobState.Queued, job.AcceptedAt);
        }
        catch
        {
            DeleteDirectoryQuietly(jobDirectory);
            throw;
        }
    }

    public DocumentJobStatus? GetStatus(string jobId)
    {
        return _jobs.TryGetValue(jobId, out var job) ? job.GetStatus() : null;
    }

    public DocumentProcessingResult? GetResult(string jobId)
    {
        return _jobs.TryGetValue(jobId, out var job) ? job.GetResult() : null;
    }

    public Task<bool> DeleteAsync(string jobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_jobs.TryRemove(jobId, out var job))
        {
            return Task.FromResult(false);
        }

        job.Cancellation.Cancel();
        DeleteDirectoryQuietly(Path.GetDirectoryName(job.InputPath)!);
        return Task.FromResult(true);
    }

    public async Task ProcessAsync(string jobId, CancellationToken cancellationToken)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            return;
        }

        job.MarkProcessing();
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, job.Cancellation.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.JobTimeoutSeconds));
            await using var content = File.OpenRead(job.InputPath);
            var result = await _provider.ProcessAsync(
                new DocumentProcessingRequest(job.Document, content),
                timeout.Token);
            job.MarkReady(result);
        }
        catch (OperationCanceledException) when (job.Cancellation.IsCancellationRequested)
        {
            job.MarkFailed("JOB_CANCELLED");
        }
        catch (OperationCanceledException)
        {
            job.MarkFailed("PROCESSING_TIMEOUT");
        }
        catch (DocumentProcessingException ex)
        {
            _logger.LogWarning("Document job {JobId} failed with {Code}", jobId, ex.Code);
            job.MarkFailed(ex.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Document job {JobId} failed unexpectedly", jobId);
            job.MarkFailed("PROCESSING_FAILED");
        }
        finally
        {
            DeleteDirectoryQuietly(Path.GetDirectoryName(job.InputPath)!);
        }
    }

    internal async Task<string> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }

    private void ValidateDocument(DocumentDescriptor document)
    {
        if (document.SizeBytes <= 0)
        {
            throw new DocumentProcessingException("EMPTY_FILE", "The uploaded document is empty.");
        }

        if (document.SizeBytes > _options.MaxFileSizeBytes)
        {
            throw new DocumentProcessingException("FILE_TOO_LARGE", $"The document exceeds the {_options.MaxFileSizeBytes}-byte limit.");
        }

        if (!string.Equals(Path.GetExtension(document.FileName), ".pdf", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(document.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new DocumentProcessingException("UNSUPPORTED_FILE_TYPE", "Only PDF documents are supported.");
        }

        if (!_provider.CanProcess(document))
        {
            throw new DocumentProcessingException("NO_PROVIDER", "No document processing provider supports this document.");
        }
    }

    private static void DeleteDirectoryQuietly(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // The next cleanup cycle or container restart can remove a residual work directory.
        }
    }
}
