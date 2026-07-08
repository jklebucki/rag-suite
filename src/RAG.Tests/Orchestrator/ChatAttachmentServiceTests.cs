using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Features.Chat.Attachments;
using RAG.Orchestrator.Api.Models;
using RAG.Orchestrator.Api.Services;
using System.Text;

namespace RAG.Tests.Orchestrator;

public class ChatAttachmentServiceTests : IDisposable
{
    private const string UserId = "user-1";
    private const string SessionId = "session-1";

    private readonly ChatDbContext _context;

    public ChatAttachmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChatDbContext(options);
        _context.ChatSessions.Add(new ChatSession
        {
            Id = SessionId,
            UserId = UserId,
            Title = "Test chat",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task UploadAsync_WithReadableTextFile_SavesDraftAndReturnsContextUsage()
    {
        var draftState = new List<ChatAttachmentDraft>();
        var store = CreateStoreMock(draftState);
        var service = CreateService(store, tokenCount: 5);

        var response = await service.UploadAsync(
            UserId,
            SessionId,
            Files(CreateFormFile("notes.md", "# Notes\nReadable text.")));

        store.Verify(s => s.SaveBatchAsync(UserId, SessionId, It.IsAny<IEnumerable<ChatAttachmentFile>>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(response.ContextUsage.Attachments);
        Assert.Equal(5, response.ContextUsage.AttachmentTokens);
        Assert.Equal(5, response.ContextUsage.UsedTokens);
    }

    [Fact]
    public async Task UploadAsync_WithBinaryFileUsingTextExtension_RejectsWithoutSaving()
    {
        var store = CreateStoreMock();
        var service = CreateService(store, tokenCount: 5);

        var exception = await Assert.ThrowsAsync<ChatAttachmentException>(() =>
            service.UploadAsync(UserId, SessionId, Files(CreateFormFile("payload.txt", new byte[] { 65, 0, 66 }))));

        Assert.Equal("BINARY_FILE", exception.Code);
        store.Verify(s => s.SaveBatchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<ChatAttachmentFile>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_WithUnsupportedExtension_RejectsWithoutSaving()
    {
        var store = CreateStoreMock();
        var service = CreateService(store, tokenCount: 5);

        var exception = await Assert.ThrowsAsync<ChatAttachmentException>(() =>
            service.UploadAsync(UserId, SessionId, Files(CreateFormFile("manual.pdf", "not really a pdf"))));

        Assert.Equal("UNSUPPORTED_FILE_TYPE", exception.Code);
        store.Verify(s => s.SaveBatchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<ChatAttachmentFile>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadAsync_WhenBatchExceedsAttachmentTokenLimit_DoesNotSavePartialFiles()
    {
        var store = CreateStoreMock();
        var service = CreateService(store, tokenCount: 10, attachmentLimit: 15);

        var exception = await Assert.ThrowsAsync<ChatAttachmentException>(() =>
            service.UploadAsync(
                UserId,
                SessionId,
                Files(
                    CreateFormFile("one.txt", "first file"),
                    CreateFormFile("two.txt", "second file"))));

        Assert.Equal("ATTACHMENT_CONTEXT_LIMIT_EXCEEDED", exception.Code);
        store.Verify(s => s.SaveBatchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<ChatAttachmentFile>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PrepareForMessageAsync_WhenSessionLimitExceeded_ThrowsWithoutWritingMessage()
    {
        _context.ChatMessages.Add(new ChatMessage
        {
            Id = "message-1",
            SessionId = SessionId,
            Role = "user",
            Content = "existing",
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var store = CreateStoreMock();
        var service = CreateService(store, tokenCountSelector: text => text == "existing" ? 5 : 1, sessionLimit: 5);

        var exception = await Assert.ThrowsAsync<ChatAttachmentException>(() =>
            service.PrepareForMessageAsync(UserId, SessionId, "next", Array.Empty<string>()));

        Assert.Equal("SESSION_CONTEXT_LIMIT_EXCEEDED", exception.Code);
        Assert.Equal(1, await _context.ChatMessages.CountAsync());
    }

    [Fact]
    public void BuildAttachmentsPromptBlock_IncludesSafetyInstructionAndFileMetadata()
    {
        var block = ChatAttachmentService.BuildAttachmentsPromptBlock(new[]
        {
            new ChatAttachmentFile("file-1", "notes.md", "text/markdown", 32, 7, "# Notes")
        });

        Assert.Contains("=== USER ATTACHED FILES ===", block);
        Assert.Contains("untrusted context", block);
        Assert.Contains("--- FILE: notes.md ---", block);
        Assert.Contains("Content-Type: text/markdown", block);
        Assert.Contains("Estimated tokens: 7", block);
        Assert.Contains("# Notes", block);
    }

    private ChatAttachmentService CreateService(
        Mock<IChatAttachmentStore> store,
        int tokenCount = 1,
        int attachmentLimit = 12000,
        int sessionLimit = 9600)
    {
        return CreateService(store, _ => tokenCount, attachmentLimit, sessionLimit);
    }

    private ChatAttachmentService CreateService(
        Mock<IChatAttachmentStore> store,
        Func<string, int> tokenCountSelector,
        int attachmentLimit = 12000,
        int sessionLimit = 9600)
    {
        var settings = new LlmSettings
        {
            ContextWindow = 98000,
            AttachmentContextLimitTokens = attachmentLimit,
            SessionContextLimitTokens = sessionLimit
        };

        var settingsService = new Mock<IGlobalSettingsService>();
        settingsService.Setup(s => s.GetLlmSettingsAsync()).ReturnsAsync(settings);

        var tokenCounter = new Mock<IContextTokenCounter>();
        tokenCounter
            .Setup(counter => counter.CountTokens(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns((string text, string? _) => tokenCountSelector(text));

        return new ChatAttachmentService(_context, settingsService.Object, tokenCounter.Object, store.Object);
    }

    private static Mock<IChatAttachmentStore> CreateStoreMock(List<ChatAttachmentDraft>? draftState = null)
    {
        draftState ??= new List<ChatAttachmentDraft>();
        var store = new Mock<IChatAttachmentStore>();

        store
            .Setup(s => s.GetDraftsAsync(UserId, SessionId, It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(draftState.ToArray()));

        store
            .Setup(s => s.GetFilesAsync(UserId, SessionId, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ChatAttachmentFile>());

        store
            .Setup(s => s.SaveBatchAsync(UserId, SessionId, It.IsAny<IEnumerable<ChatAttachmentFile>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IEnumerable<ChatAttachmentFile>, CancellationToken>((_, _, files, _) =>
            {
                draftState.AddRange(files.Select(file => new ChatAttachmentDraft(
                    file.Id,
                    file.FileName,
                    file.ContentType,
                    file.SizeBytes,
                    file.TokenCount,
                    DateTimeOffset.UtcNow)));
            })
            .Returns(Task.CompletedTask);

        store
            .Setup(s => s.RemoveBatchAsync(UserId, SessionId, It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return store;
    }

    private static FormFileCollection Files(params IFormFile[] files)
    {
        var collection = new FormFileCollection();
        foreach (var file in files)
        {
            collection.Add(file);
        }

        return collection;
    }

    private static IFormFile CreateFormFile(string fileName, string content)
    {
        return CreateFormFile(fileName, Encoding.UTF8.GetBytes(content));
    }

    private static IFormFile CreateFormFile(string fileName, byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "files", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };
    }
}
