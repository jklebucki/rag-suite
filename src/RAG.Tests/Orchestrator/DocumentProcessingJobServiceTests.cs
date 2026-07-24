using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RAG.DocumentProcessing.Abstractions;
using RAG.DocumentProcessing.Core;
using System.Text;

namespace RAG.Tests.Orchestrator;

public class DocumentProcessingJobServiceTests
{
    [Fact]
    public async Task SubmitAsync_WhenUploadDoesNotHavePdfHeader_RejectsIt()
    {
        await using var fixture = await JobServiceFixture.CreateAsync();
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("not a PDF"));

        var exception = await Assert.ThrowsAsync<DocumentProcessingException>(() =>
            fixture.Service.SubmitAsync(new DocumentDescriptor("file.pdf", "application/pdf", content.Length), content, CancellationToken.None));

        Assert.Equal("INVALID_PDF", exception.Code);
    }

    [Fact]
    public async Task SubmitAsync_WhenPdfExceedsPageLimit_RejectsIt()
    {
        await using var fixture = await JobServiceFixture.CreateAsync(maxPageCount: 1);
        await using var content = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.7\n/Type /Page\n/Type /Page"));

        var exception = await Assert.ThrowsAsync<DocumentProcessingException>(() =>
            fixture.Service.SubmitAsync(new DocumentDescriptor("large.pdf", "application/pdf", content.Length), content, CancellationToken.None));

        Assert.Equal("PAGE_LIMIT_EXCEEDED", exception.Code);
    }

    private sealed class JobServiceFixture : IAsyncDisposable
    {
        private JobServiceFixture(DocumentProcessingJobService service, string contentRootPath)
        {
            Service = service;
            ContentRootPath = contentRootPath;
        }

        public DocumentProcessingJobService Service { get; }

        private string ContentRootPath { get; }

        public static Task<JobServiceFixture> CreateAsync(int maxPageCount = 100)
        {
            var contentRootPath = Path.Combine(Path.GetTempPath(), "rag-suite-document-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(contentRootPath);
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(item => item.ContentRootPath).Returns(contentRootPath);
            var provider = new Mock<IDocumentProcessingProvider>();
            provider.SetupGet(item => item.Name).Returns("test");
            provider.Setup(item => item.CanProcess(It.IsAny<DocumentDescriptor>())).Returns(true);
            var options = Options.Create(new DocumentProcessingOptions
            {
                WorkDirectory = "jobs",
                MaxPageCount = maxPageCount,
                MaxQueuedJobs = 2,
                MaxConcurrentJobs = 1
            });
            var service = new DocumentProcessingJobService(provider.Object, options, environment.Object, NullLogger<DocumentProcessingJobService>.Instance);
            return Task.FromResult(new JobServiceFixture(service, contentRootPath));
        }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(ContentRootPath))
            {
                Directory.Delete(ContentRootPath, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
