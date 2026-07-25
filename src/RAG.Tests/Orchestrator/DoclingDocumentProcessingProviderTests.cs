using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RAG.DocumentProcessing.Abstractions;
using RAG.DocumentProcessing.Providers.Docling;
using System.Net;
using System.Text;
using System.Text.Json;

namespace RAG.Tests.Orchestrator;

public class DoclingDocumentProcessingProviderTests
{
    [Fact]
    public async Task ProcessAsync_WhenInitialExtractionIsEmpty_RetriesWithForcedOcr()
    {
        var handler = new DoclingHandler(string.Empty, "OCR result");
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://docling/") };
        var provider = new DoclingDocumentProcessingProvider(
            httpClient,
            Options.Create(new DoclingOptions
            {
                Endpoint = "http://docling/",
                PollIntervalMilliseconds = 100,
                MinimumCharactersPerPage = 40
            }),
            NullLogger<DoclingDocumentProcessingProvider>.Instance);
        await using var content = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.7 test"));

        var result = await provider.ProcessAsync(
            new DocumentProcessingRequest(new DocumentDescriptor("scan.pdf", "application/pdf", content.Length), content),
            CancellationToken.None);

        Assert.Equal("OCR result", result.Markdown);
        Assert.Equal("docling", result.Provider);
        Assert.Equal(1, result.PageCount);
        Assert.Equal(2, handler.Submissions);
        Assert.Contains(handler.SubmissionBodies, body => body.Contains("force_ocr\r\n\r\ntrue", StringComparison.Ordinal));
        Assert.Contains(result.Warnings, warning => warning.Contains("force_ocr", StringComparison.Ordinal));
        Assert.Contains(handler.SubmissionBodies, body => body.Contains("ocr_preset\r\n\r\ntesseract", StringComparison.Ordinal));
        Assert.Contains(handler.SubmissionBodies, body => body.Contains("ocr_lang\r\n\r\npol", StringComparison.Ordinal));
        Assert.Contains(handler.SubmissionBodies, body => body.Contains("ocr_lang\r\n\r\neng", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProcessAsync_WhenInitialTextHasManyOrphanLetters_RetriesWithForcedOcr()
    {
        const string corrupted = """
            n on sn n n 'zkie prawa do korzystania z programu dotychczas posiadanego
            akt iid i i i i yne przysługują nabywcy wyłącznie na aktualnie posiadany program.
            eid e id s d d e z zd z z zdk rozbudowy aktualnie posiadanego programu.
            """;
        var coherent = string.Concat(Enumerable.Repeat(
            "Oświadczam, że wszystkie prawa do programu pozostają zgodne z umową. ",
            10));
        var handler = new DoclingHandler(corrupted, coherent);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://docling/") };
        var provider = new DoclingDocumentProcessingProvider(
            httpClient,
            Options.Create(new DoclingOptions
            {
                Endpoint = "http://docling/",
                PollIntervalMilliseconds = 100,
                MinimumTextQuality = 0.6d
            }),
            NullLogger<DoclingDocumentProcessingProvider>.Instance);
        await using var content = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.7 test"));

        var result = await provider.ProcessAsync(
            new DocumentProcessingRequest(new DocumentDescriptor("scan.pdf", "application/pdf", content.Length), content),
            CancellationToken.None);

        Assert.Equal(2, handler.Submissions);
        Assert.Equal(coherent, result.Markdown);
        Assert.Contains(result.Warnings, warning => warning.Contains("text quality was low", StringComparison.Ordinal));
        Assert.Contains("force_ocr\r\n\r\ntrue", handler.SubmissionBodies[1], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessAsync_WhenPrimaryTableIsIncomplete_MergesFallbackTableWithoutReplacingProse()
    {
        const string primaryTable = """
            | Nazwa | Numer | Limit |
            | --- | --- | --- |
            | Handel | HMF-100790 | |
            | Finanse | FKF-101358 | |
            """;
        const string fallbackTable = """
            | Nazwa | Numer | Limit |
            | --- | --- | --- |
            | Handel | HMF-100790 | 6 |
            | Finanse | FKF-101358 | 19 |
            """;
        var primaryProse = string.Concat(Enumerable.Repeat(
            "Oświadczam, że wszystkie prawa do programu pozostają zgodne z umową. ",
            10));
        var handler = new DoclingHandler(
            $"{primaryProse}\n\n{primaryTable}",
            $"uszkodzony tekst zapasowy\n\n{fallbackTable}");
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://docling/") };
        var provider = new DoclingDocumentProcessingProvider(
            httpClient,
            Options.Create(new DoclingOptions
            {
                Endpoint = "http://docling/",
                PollIntervalMilliseconds = 100,
                OcrPreset = "tesseract",
                OcrLanguages = "pol,eng",
                TableFallbackOcrPreset = "auto",
                MinimumTableCompleteness = 0.85d
            }),
            NullLogger<DoclingDocumentProcessingProvider>.Instance);
        await using var content = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.7 test"));

        var result = await provider.ProcessAsync(
            new DocumentProcessingRequest(new DocumentDescriptor("scan.pdf", "application/pdf", content.Length), content),
            CancellationToken.None);

        Assert.Equal(2, handler.Submissions);
        Assert.StartsWith(primaryProse, result.Markdown, StringComparison.Ordinal);
        Assert.Contains("| Handel | HMF-100790 | 6 |", result.Markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("uszkodzony tekst zapasowy", result.Markdown, StringComparison.Ordinal);
        Assert.Contains(result.Warnings, warning => warning.Contains("incomplete Markdown table", StringComparison.Ordinal));
        Assert.Contains("ocr_preset\r\n\r\ntesseract", handler.SubmissionBodies[0], StringComparison.Ordinal);
        Assert.Contains("ocr_preset\r\n\r\nauto", handler.SubmissionBodies[1], StringComparison.Ordinal);
    }

    private sealed class DoclingHandler : HttpMessageHandler
    {
        private readonly IReadOnlyList<string> _results;

        public DoclingHandler(params string[] results)
        {
            _results = results;
        }

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

            if (path.StartsWith("/v1/result/task-", StringComparison.Ordinal) &&
                int.TryParse(path["/v1/result/task-".Length..], out var taskNumber) &&
                taskNumber >= 1 &&
                taskNumber <= _results.Count)
            {
                var result = _results[taskNumber - 1];
                return Json(JsonSerializer.Serialize(new
                {
                    status = "success",
                    document = new
                    {
                        md_content = result,
                        text_content = result,
                        json_content = new { pages = new Dictionary<string, object> { ["1"] = new() } }
                    }
                }));
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
