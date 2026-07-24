using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RAG.DocumentProcessing.Abstractions;
using RAG.Orchestrator.Api.Data;
using RAG.Orchestrator.Api.Features.Chat.Artifacts;

namespace RAG.Tests.Orchestrator;

public class LocalTemporaryArtifactStoreTests : IAsyncDisposable
{
    private readonly string _contentRootPath = Path.Combine(Path.GetTempPath(), "rag-suite-artifact-tests", Guid.NewGuid().ToString("N"));
    private readonly ChatDbContext _context;
    private readonly LocalTemporaryArtifactStore _store;

    public LocalTemporaryArtifactStoreTests()
    {
        Directory.CreateDirectory(_contentRootPath);
        _context = new ChatDbContext(new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(item => item.ContentRootPath).Returns(_contentRootPath);
        _store = new LocalTemporaryArtifactStore(
            _context,
            environment.Object,
            Options.Create(new GeneratedArtifactOptions { RootDirectory = "artifacts", ExpirationHours = 12 }),
            NullLogger<LocalTemporaryArtifactStore>.Instance);
    }

    [Fact]
    public async Task Store_ProtectsArtifactsByUserAndRemovesExpiredRecordsAndFiles()
    {
        var stored = await _store.SaveAsync(
            new GeneratedArtifact(
                GeneratedArtifactFormat.Txt,
                "result.txt",
                "text/plain; charset=utf-8",
                [1, 2, 3],
                "owner",
                "session-1",
                "assistant-1"),
            CancellationToken.None);

        Assert.Null(await _store.GetAsync(stored.Id, "another-user", CancellationToken.None));
        Assert.NotNull(await _store.GetAsync(stored.Id, "owner", CancellationToken.None));

        var entity = await _context.GeneratedArtifacts.SingleAsync();
        entity.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await _context.SaveChangesAsync();

        var expired = await _store.GetAsync(stored.Id, "owner", CancellationToken.None);
        Assert.NotNull(expired);
        Assert.True(expired.ExpiresAt < DateTimeOffset.UtcNow);

        await _store.DeleteExpiredAsync(CancellationToken.None);
        Assert.Null(await _store.GetAsync(stored.Id, "owner", CancellationToken.None));
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(_contentRootPath, "artifacts"), "*", SearchOption.AllDirectories));
    }

    public ValueTask DisposeAsync()
    {
        _context.Dispose();
        if (Directory.Exists(_contentRootPath))
        {
            Directory.Delete(_contentRootPath, recursive: true);
        }

        return ValueTask.CompletedTask;
    }
}
