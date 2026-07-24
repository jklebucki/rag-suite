using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RAG.DocumentProcessing.Abstractions;
using RAG.DocumentProcessing.Providers.Docling;
using System.Net;
using System.Text;

namespace RAG.Tests.Orchestrator;

public class DoclingDocumentProcessingProviderTests
{
    [Fact]
    public async Task ProcessAsync_WhenInitialExtractionIsEmpty_RetriesWithForcedOcr()
    {
        var handler = new DoclingHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://docling/") };
        var provider = new DoclingDocumentProcessingProvider(
            httpClient,
            Options.Create(new DoclingOptions { Endpoint = "http://docling/", PollIntervalMilliseconds = 100, MinimumCharactersPerPage = 40 }),
            NullLogger<DoclingDocumentProcessingProvider>.Instance);
        await using var content = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.7 test"));

        var result = await provider.ProcessAsync(
            new DocumentProcessingRequest(new DocumentDescriptor("scan.pdf", "application/pdf", content.Length), content),
            CancellationToken.None);

        Assert.Equal("OCR result", result.Markdown);
        Assert.Equal("docling", result.Provider);
        Assert.Equal(2, handler.Submissions);
        Assert.Contains(handler.SubmissionBodies, body => body.Contains("force_ocr\r\n\r\ntrue", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, warning => warning.Contains("force_ocr", StringComparison.Ordinal));
    }

    private sealed class DoclingHandler : HttpMessageHandler
    {
        public int Submissions { get; private set; }

        public List<string> SubmissionBodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Post && path == "/v1/convert/file/async")
            {
                Submissions++;
                SubmissionBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
                return Json($"{{\"task_id\":\"task-{Submissions}\"}}");
            }

            if (path.StartsWith("/v1/status/poll/", StringComparison.Ordinal))
            {
                return Json("{\"task_status\":\"success\"}");
            }

            if (path == "/v1/result/task-1")
            {
                return Json("{\"status\":\"success\",\"document\":{\"md_content\":\"\",\"text_content\":\"\",\"json_content\":{\"pages\":[{}]}}}");
            }

            if (path == "/v1/result/task-2")
            {
                return Json("{\"status\":\"success\",\"document\":{\"md_content\":\"OCR result\",\"text_content\":\"OCR result\",\"json_content\":{\"pages\":[{}]}}}");
            }

            throw new InvalidOperationException($"Unexpected Docling request: {request.Method} {path}");
        }

        private static HttpResponseMessage Json(string content)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };
        }
    }
}
