using Microsoft.EntityFrameworkCore;
using RAG.DocumentProcessing.Abstractions;
using RAG.Orchestrator.Api.Common.Constants;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Models;
using RAG.Orchestrator.Api.Services;
using System.Text;
using System.Text.Json;

namespace RAG.Orchestrator.Api.Features.Chat.Attachments;

public class ChatAttachmentService : IChatAttachmentService
{
    private const int MaxDraftAttachments = 5;
    private const int MaxTextFileSizeBytes = 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".markdown", ".csv", ".tsv", ".json", ".yaml", ".yml", ".xml", ".log", ".ini", ".env",
        ".sql", ".html", ".htm", ".css", ".js", ".jsx", ".ts", ".tsx", ".cs", ".py", ".sh", ".ps1"
    };

    private readonly ChatDbContext _chatDbContext;
    private readonly IGlobalSettingsService _globalSettingsService;
    private readonly IContextTokenCounter _tokenCounter;
    private readonly IChatAttachmentStore _attachmentStore;
    private readonly IDocumentProcessingClient _documentProcessingClient;

    public ChatAttachmentService(
        ChatDbContext chatDbContext,
        IGlobalSettingsService globalSettingsService,
        IContextTokenCounter tokenCounter,
        IChatAttachmentStore attachmentStore,
        IDocumentProcessingClient documentProcessingClient)
    {
        _chatDbContext = chatDbContext;
        _globalSettingsService = globalSettingsService;
        _tokenCounter = tokenCounter;
        _attachmentStore = attachmentStore;
        _documentProcessingClient = documentProcessingClient;
    }

    public async Task<ChatContextUsageResponse?> GetContextAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
    {
        if (!await SessionExistsAsync(userId, sessionId, cancellationToken))
        {
            return null;
        }

        await RefreshDocumentProcessingAsync(userId, sessionId, cancellationToken);
        return await BuildContextUsageAsync(userId, sessionId, includeDraftAttachments: true, cancellationToken);
    }

    public async Task<ChatAttachmentUploadResponse> UploadAsync(string userId, string sessionId, IFormFileCollection files, CancellationToken cancellationToken = default)
    {
        if (!await SessionExistsAsync(userId, sessionId, cancellationToken))
        {
            throw new ChatAttachmentException("SESSION_NOT_FOUND", "Session not found or access denied.");
        }

        if (files.Count == 0)
        {
            throw new ChatAttachmentException("NO_FILES", "Select at least one file.");
        }

        await RefreshDocumentProcessingAsync(userId, sessionId, cancellationToken);
        var currentDrafts = await _attachmentStore.GetDraftsAsync(userId, sessionId, cancellationToken);
        if (currentDrafts.Length + files.Count > MaxDraftAttachments)
        {
            throw new ChatAttachmentException("FILE_COUNT_EXCEEDED", $"You can attach up to {MaxDraftAttachments} files.");
        }

        var limits = await GetLimitsAsync();
        var preparedFiles = new List<ChatAttachmentFile>();
        foreach (var file in files.Where(file => !IsPdf(file)))
        {
            preparedFiles.Add(await ValidateAndPrepareTextFileAsync(file, limits.Model, cancellationToken));
        }

        var currentDraftTokens = currentDrafts.Where(IsReady).Sum(attachment => attachment.TokenCount);
        var newTokens = preparedFiles.Sum(file => file.TokenCount);
        if (currentDraftTokens + newTokens > limits.AttachmentContextLimitTokens)
        {
            throw new ChatAttachmentException(
                "ATTACHMENT_CONTEXT_LIMIT_EXCEEDED",
                $"These files would use {currentDraftTokens + newTokens} attachment tokens. The limit is {limits.AttachmentContextLimitTokens}.");
        }

        var persistedSessionTokens = await CountPersistedSessionTokensAsync(sessionId, limits.Model, cancellationToken);
        var remainingSessionTokens = Math.Max(0, limits.SessionContextLimitTokens - persistedSessionTokens - currentDraftTokens);
        if (newTokens > remainingSessionTokens)
        {
            throw new ChatAttachmentException(
                "SESSION_CONTEXT_LIMIT_EXCEEDED",
                $"These files do not fit in the remaining session context. Remaining: {remainingSessionTokens} tokens.");
        }

        foreach (var file in files.Where(IsPdf))
        {
            preparedFiles.Add(await SubmitPdfAsync(file, cancellationToken));
        }

        await _attachmentStore.SaveBatchAsync(userId, sessionId, preparedFiles, cancellationToken);
        var contextUsage = await BuildContextUsageAsync(userId, sessionId, includeDraftAttachments: true, cancellationToken);
        return new ChatAttachmentUploadResponse(contextUsage);
    }

    public async Task<bool> DeleteAsync(string userId, string sessionId, string attachmentId, CancellationToken cancellationToken = default)
    {
        if (!await SessionExistsAsync(userId, sessionId, cancellationToken))
        {
            return false;
        }

        return await _attachmentStore.RemoveAsync(userId, sessionId, attachmentId, cancellationToken);
    }

    public async Task<PreparedChatAttachments> PrepareForMessageAsync(string userId, string sessionId, string message, IEnumerable<string>? attachmentIds, CancellationToken cancellationToken = default)
    {
        if (!await SessionExistsAsync(userId, sessionId, cancellationToken))
        {
            throw new ChatAttachmentException("SESSION_NOT_FOUND", "Session not found or access denied.");
        }

        await RefreshDocumentProcessingAsync(userId, sessionId, cancellationToken);
        var selectedIds = NormalizeAttachmentIds(attachmentIds);
        var selectedFiles = await _attachmentStore.GetFilesAsync(userId, sessionId, selectedIds, cancellationToken);
        if (selectedIds.Length != selectedFiles.Length)
        {
            throw new ChatAttachmentException("ATTACHMENT_NOT_FOUND", "One or more selected attachments no longer exist.");
        }

        var nonReady = selectedFiles.FirstOrDefault(file => !IsReady(file));
        if (nonReady != null)
        {
            throw new ChatAttachmentException(
                "ATTACHMENT_NOT_READY",
                $"Attachment '{nonReady.FileName}' is {nonReady.Status} and cannot be sent yet.");
        }

        var limits = await GetLimitsAsync();
        var persistedSessionTokens = await CountPersistedSessionTokensAsync(sessionId, limits.Model, cancellationToken);
        var messageTokens = _tokenCounter.CountTokens(message, limits.Model);
        var attachmentTokens = selectedFiles.Sum(file => file.TokenCount);
        var requestedTokens = persistedSessionTokens + messageTokens + attachmentTokens;

        if (persistedSessionTokens >= limits.SessionContextLimitTokens || requestedTokens > limits.SessionContextLimitTokens)
        {
            throw new ChatAttachmentException(
                "SESSION_CONTEXT_LIMIT_EXCEEDED",
                $"This session has reached the context limit ({requestedTokens}/{limits.SessionContextLimitTokens} tokens). Start a new chat to continue.");
        }

        var usage = await BuildContextUsageAsync(userId, sessionId, includeDraftAttachments: true, cancellationToken);
        return new PreparedChatAttachments(selectedFiles, attachmentTokens, usage);
    }

    public async Task CommitMessageAttachmentsAsync(string userId, string sessionId, IEnumerable<string>? attachmentIds, CancellationToken cancellationToken = default)
    {
        await _attachmentStore.RemoveBatchAsync(userId, sessionId, NormalizeAttachmentIds(attachmentIds), cancellationToken);
    }

    public async Task ClearSessionAsync(string userId, string sessionId, CancellationToken cancellationToken = default)
    {
        await _attachmentStore.ClearSessionAsync(userId, sessionId, cancellationToken);
    }

    private async Task<ChatContextUsageResponse> BuildContextUsageAsync(string userId, string sessionId, bool includeDraftAttachments, CancellationToken cancellationToken)
    {
        var limits = await GetLimitsAsync();
        var persistedTokens = await CountPersistedSessionTokensAsync(sessionId, limits.Model, cancellationToken);
        var attachments = includeDraftAttachments
            ? await _attachmentStore.GetDraftsAsync(userId, sessionId, cancellationToken)
            : Array.Empty<ChatAttachmentDraft>();

        var attachmentTokens = attachments.Where(IsReady).Sum(attachment => attachment.TokenCount);
        var usedTokens = persistedTokens + attachmentTokens;
        var percent = limits.SessionContextLimitTokens <= 0
            ? 100
            : Math.Clamp((int)Math.Ceiling(usedTokens * 100.0 / limits.SessionContextLimitTokens), 0, 100);

        return new ChatContextUsageResponse(
            usedTokens,
            limits.SessionContextLimitTokens,
            percent,
            usedTokens >= limits.SessionContextLimitTokens,
            attachmentTokens,
            limits.AttachmentContextLimitTokens,
            attachments);
    }

    private async Task<int> CountPersistedSessionTokensAsync(string sessionId, string model, CancellationToken cancellationToken)
    {
        var messages = await _chatDbContext.ChatMessages
            .Where(message => message.SessionId == sessionId)
            .OrderBy(message => message.Timestamp)
            .Select(message => new
            {
                message.Content,
                message.MetadataJson
            })
            .ToListAsync(cancellationToken);

        var total = 0;
        foreach (var message in messages)
        {
            total += _tokenCounter.CountTokens(message.Content, model);
            total += ExtractAttachmentTokens(message.MetadataJson);
        }

        return total;
    }

    private async Task<ChatAttachmentFile> ValidateAndPrepareTextFileAsync(IFormFile file, string model, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ChatAttachmentException("INVALID_FILE_NAME", "File name is invalid.");
        }

        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new ChatAttachmentException("UNSUPPORTED_FILE_TYPE", $"File type '{extension}' is not supported.");
        }

        if (file.Length <= 0)
        {
            throw new ChatAttachmentException("EMPTY_FILE", $"File '{fileName}' is empty.");
        }

        if (file.Length > MaxTextFileSizeBytes)
        {
            throw new ChatAttachmentException("FILE_TOO_LARGE", $"Text file '{fileName}' is larger than 1 MB.");
        }

        byte[] bytes;
        await using (var stream = file.OpenReadStream())
        using (var memory = new MemoryStream())
        {
            await stream.CopyToAsync(memory, cancellationToken);
            bytes = memory.ToArray();
        }

        if (LooksBinary(bytes))
        {
            throw new ChatAttachmentException("BINARY_FILE", $"File '{fileName}' does not look like readable text.");
        }

        string content;
        try
        {
            content = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            throw new ChatAttachmentException("UNREADABLE_ENCODING", $"File '{fileName}' must be readable UTF-8 text.");
        }

        if (HasTooManyControlCharacters(content))
        {
            throw new ChatAttachmentException("BINARY_FILE", $"File '{fileName}' contains too many non-text characters.");
        }

        var tokenCount = CountAttachmentTokens(fileName, file.ContentType, file.Length, content, model);
        return new ChatAttachmentFile(
            Guid.NewGuid().ToString(),
            fileName,
            string.IsNullOrWhiteSpace(file.ContentType) ? "text/plain" : file.ContentType,
            file.Length,
            tokenCount,
            content);
    }

    private async Task<ChatAttachmentFile> SubmitPdfAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(fileName) || !string.Equals(Path.GetExtension(fileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ChatAttachmentException("UNSUPPORTED_FILE_TYPE", "Only PDF documents are supported by document processing.");
        }

        if (file.Length <= 0)
        {
            throw new ChatAttachmentException("EMPTY_FILE", $"File '{fileName}' is empty.");
        }

        var contentType = "application/pdf";
        try
        {
            await using var content = file.OpenReadStream();
            var job = await _documentProcessingClient.SubmitAsync(content, fileName, contentType, cancellationToken);
            return new ChatAttachmentFile(
                Guid.NewGuid().ToString(),
                fileName,
                contentType,
                file.Length,
                0,
                string.Empty,
                Status: ToStatusValue(job.Status),
                Progress: 0,
                DocumentJobId: job.JobId);
        }
        catch (DocumentProcessingException ex)
        {
            throw new ChatAttachmentException(ex.Code, ex.Message);
        }
    }

    private async Task RefreshDocumentProcessingAsync(string userId, string sessionId, CancellationToken cancellationToken)
    {
        var drafts = await _attachmentStore.GetDraftsAsync(userId, sessionId, cancellationToken);
        var pendingIds = drafts
            .Where(draft => !string.IsNullOrWhiteSpace(draft.DocumentJobId))
            .Select(draft => draft.Id)
            .ToArray();
        if (pendingIds.Length == 0)
        {
            return;
        }

        var files = await _attachmentStore.GetFilesAsync(userId, sessionId, pendingIds, cancellationToken);
        var limits = await GetLimitsAsync();
        var persistedTokens = await CountPersistedSessionTokensAsync(sessionId, limits.Model, cancellationToken);
        var readyTokens = drafts.Where(IsReady).Sum(draft => draft.TokenCount);

        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file.DocumentJobId))
            {
                continue;
            }

            try
            {
                var job = await _documentProcessingClient.GetStatusAsync(file.DocumentJobId, cancellationToken);
                var status = ToStatusValue(job.Status);
                if (job.Status is DocumentJobState.Queued or DocumentJobState.Processing)
                {
                    await _attachmentStore.UpdateAsync(
                        userId,
                        sessionId,
                        file with
                        {
                            Status = status,
                            Progress = job.Progress,
                            PageCount = job.PageCount,
                            Provider = job.Provider,
                            ErrorCode = null
                        },
                        cancellationToken);
                    continue;
                }

                if (job.Status == DocumentJobState.Failed)
                {
                    await _attachmentStore.UpdateAsync(
                        userId,
                        sessionId,
                        file with
                        {
                            Status = status,
                            Progress = job.Progress,
                            PageCount = job.PageCount,
                            Provider = job.Provider,
                            ErrorCode = job.ErrorCode ?? "PROCESSING_FAILED",
                            DocumentJobId = null
                        },
                        cancellationToken);
                    continue;
                }

                var result = await _documentProcessingClient.GetResultAsync(file.DocumentJobId, cancellationToken);
                var tokenCount = CountAttachmentTokens(file.FileName, file.ContentType, file.SizeBytes, result.Markdown, limits.Model);
                var exceedsAttachmentLimit = readyTokens + tokenCount > limits.AttachmentContextLimitTokens;
                var exceedsSessionLimit = persistedTokens + readyTokens + tokenCount > limits.SessionContextLimitTokens;
                if (exceedsAttachmentLimit || exceedsSessionLimit)
                {
                    await _attachmentStore.UpdateAsync(
                        userId,
                        sessionId,
                        file with
                        {
                            Status = "failed",
                            Progress = 100,
                            PageCount = result.PageCount > 0 ? result.PageCount : job.PageCount,
                            Provider = result.Provider,
                            ErrorCode = exceedsAttachmentLimit ? "ATTACHMENT_CONTEXT_LIMIT_EXCEEDED" : "SESSION_CONTEXT_LIMIT_EXCEEDED",
                            DocumentJobId = null
                        },
                        cancellationToken);
                    continue;
                }

                readyTokens += tokenCount;
                await _attachmentStore.UpdateAsync(
                    userId,
                    sessionId,
                    file with
                    {
                        Content = result.Markdown,
                        TokenCount = tokenCount,
                        Status = "ready",
                        Progress = 100,
                        PageCount = result.PageCount > 0 ? result.PageCount : job.PageCount,
                        Provider = result.Provider,
                        ErrorCode = null,
                        DocumentJobId = null
                    },
                    cancellationToken);
            }
            catch (DocumentProcessingException)
            {
                // Keep the current draft state. A temporary service outage must not erase the document result.
            }
        }
    }

    private int CountAttachmentTokens(string fileName, string contentType, long sizeBytes, string content, string model)
    {
        var injectedBlock = BuildAttachmentPromptBlock(new ChatAttachmentFile(
            "draft",
            fileName,
            string.IsNullOrWhiteSpace(contentType) ? "text/plain" : contentType,
            sizeBytes,
            0,
            content));

        return _tokenCounter.CountTokens(injectedBlock, model);
    }

    public static string BuildAttachmentsPromptBlock(IEnumerable<ChatAttachmentFile> files)
    {
        var fileArray = files.ToArray();
        if (fileArray.Length == 0)
        {
            return string.Empty;
        }

        // Data only: the safety instruction (treat as untrusted, ignore embedded instructions)
        // lives in the system_*.md files under the "Runtime Context And Untrusted Input" section.
        var builder = new StringBuilder();
        builder.AppendLine("=== USER ATTACHED FILES ===");
        builder.AppendLine();

        foreach (var file in fileArray)
        {
            builder.Append(BuildAttachmentPromptBlock(file));
            builder.AppendLine();
        }

        builder.AppendLine("=== END USER ATTACHED FILES ===");
        return builder.ToString();
    }

    private static string BuildAttachmentPromptBlock(ChatAttachmentFile file)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"--- FILE: {file.FileName} ---");
        builder.AppendLine($"Content-Type: {file.ContentType}");
        builder.AppendLine($"Size: {file.SizeBytes} bytes");
        builder.AppendLine($"Estimated tokens: {file.TokenCount}");
        builder.AppendLine("Content:");
        builder.AppendLine(file.Content);
        builder.AppendLine($"--- END FILE: {file.FileName} ---");
        return builder.ToString();
    }

    private async Task<LlmSettings> GetLimitsAsync()
    {
        return await _globalSettingsService.GetLlmSettingsAsync() ?? new LlmSettings
        {
            ContextWindow = 98000,
            AttachmentContextLimitTokens = 12000,
            SessionContextLimitTokens = 9600
        };
    }

    private async Task<bool> SessionExistsAsync(string userId, string sessionId, CancellationToken cancellationToken)
    {
        return await _chatDbContext.ChatSessions
            .AnyAsync(session => session.Id == sessionId && session.UserId == userId, cancellationToken);
    }

    private static string[] NormalizeAttachmentIds(IEnumerable<string>? attachmentIds)
    {
        return (attachmentIds ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsPdf(IFormFile file)
    {
        return string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsReady(ChatAttachmentDraft attachment)
    {
        return string.Equals(attachment.Status, "ready", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsReady(ChatAttachmentFile attachment)
    {
        return string.Equals(attachment.Status, "ready", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToStatusValue(DocumentJobState status)
    {
        return status.ToString().ToLowerInvariant();
    }

    private static bool LooksBinary(byte[] bytes)
    {
        return bytes.Any(value => value == 0);
    }

    private static bool HasTooManyControlCharacters(string content)
    {
        if (content.Length == 0)
        {
            return false;
        }

        var controlCount = content.Count(ch => char.IsControl(ch) && ch is not '\r' and not '\n' and not '\t');
        return controlCount > 0 && controlCount / (double)content.Length > 0.01;
    }

    private static int ExtractAttachmentTokens(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return 0;
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (document.RootElement.TryGetProperty("attachmentsTokenCount", out var tokenElement) &&
                tokenElement.TryGetInt32(out var tokenCount))
            {
                return tokenCount;
            }
        }
        catch
        {
            return 0;
        }

        return 0;
    }
}
