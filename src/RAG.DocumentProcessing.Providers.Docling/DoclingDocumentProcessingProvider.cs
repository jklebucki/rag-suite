using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RAG.DocumentProcessing.Abstractions;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RAG.DocumentProcessing.Providers.Docling;

public sealed class DoclingDocumentProcessingProvider : IDocumentProcessingProvider
{
    private readonly HttpClient _httpClient;
    private readonly DoclingOptions _options;
    private readonly ILogger<DoclingDocumentProcessingProvider> _logger;

    public DoclingDocumentProcessingProvider(
        HttpClient httpClient,
        IOptions<DoclingOptions> options,
        ILogger<DoclingDocumentProcessingProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "docling";

    public bool CanProcess(DocumentDescriptor document)
    {
        return string.Equals(Path.GetExtension(document.FileName), ".pdf", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(document.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<DocumentProcessingResult> ProcessAsync(
        DocumentProcessingRequest request,
        CancellationToken cancellationToken)
    {
        var initial = await ProcessOnceAsync(request, forceOcr: request.ForceOcr, cancellationToken);
        if (request.ForceOcr || !RequiresForcedOcr(initial))
        {
            return initial;
        }

        if (request.Content.CanSeek)
        {
            request.Content.Position = 0;
        }

        _logger.LogInformation("Retrying document {FileName} with force_ocr=true", request.Document.FileName);
        var retried = await ProcessOnceAsync(request, forceOcr: true, cancellationToken);
        return retried with
        {
            Warnings = retried.Warnings.Append("The initial extraction was retried with force_ocr=true.").ToArray()
        };
    }

    private async Task<DocumentProcessingResult> ProcessOnceAsync(
        DocumentProcessingRequest request,
        bool forceOcr,
        CancellationToken cancellationToken)
    {
        var taskId = await SubmitAsync(request, forceOcr, cancellationToken);
        JsonDocument? statusDocument = null;
        try
        {
            while (true)
            {
                statusDocument?.Dispose();
                await Task.Delay(_options.PollIntervalMilliseconds, cancellationToken);
                statusDocument = await GetJsonAsync($"v1/status/poll/{Uri.EscapeDataString(taskId)}", cancellationToken);
                var status = GetString(statusDocument.RootElement, "task_status");
                if (string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (string.Equals(status, "failure", StringComparison.OrdinalIgnoreCase))
                {
                    throw new DocumentProcessingException("DOCLING_CONVERSION_FAILED", GetError(statusDocument.RootElement));
                }
            }
        }
        finally
        {
            statusDocument?.Dispose();
        }

        using var resultDocument = await GetJsonAsync($"v1/result/{Uri.EscapeDataString(taskId)}", cancellationToken);
        var statusValue = GetString(resultDocument.RootElement, "status");
        if (string.Equals(statusValue, "failure", StringComparison.OrdinalIgnoreCase))
        {
            throw new DocumentProcessingException("DOCLING_CONVERSION_FAILED", GetError(resultDocument.RootElement));
        }

        var markdown = GetString(resultDocument.RootElement, "md_content") ?? string.Empty;
        var plainText = GetString(resultDocument.RootElement, "text_content") ?? markdown;
        var pageCount = GetArrayLength(resultDocument.RootElement, "pages");
        var warnings = GetStringValues(resultDocument.RootElement, "errors");
        var qualityScore = CalculateQuality(markdown, plainText, pageCount);
        return new DocumentProcessingResult(
            markdown,
            plainText,
            pageCount,
            OcrUsed: true,
            Provider: Name,
            qualityScore,
            warnings);
    }

    private async Task<string> SubmitAsync(DocumentProcessingRequest request, bool forceOcr, CancellationToken cancellationToken)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(new NonDisposingStream(request.Content));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(request.Document.ContentType);
        form.Add(fileContent, "files", request.Document.FileName);
        form.Add(new StringContent("md"), "to_formats");
        form.Add(new StringContent("text"), "to_formats");
        form.Add(new StringContent("json"), "to_formats");
        form.Add(new StringContent("true"), "do_ocr");
        form.Add(new StringContent(forceOcr ? "true" : "false"), "force_ocr");
        form.Add(new StringContent("pl"), "ocr_lang");
        form.Add(new StringContent("en"), "ocr_lang");
        form.Add(new StringContent("accurate"), "table_mode");
        form.Add(new StringContent("placeholder"), "image_export_mode");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "v1/convert/file/async")
        {
            Content = form
        };
        AddApiKey(requestMessage);
        using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        using var document = await ReadJsonAsync(response, cancellationToken);
        return GetString(document.RootElement, "task_id")
            ?? throw new DocumentProcessingException("DOCLING_INVALID_RESPONSE", "Docling did not return a task id.");
    }

    private async Task<JsonDocument> GetJsonAsync(string relativePath, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
        AddApiKey(request);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return await ReadJsonAsync(response, cancellationToken);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new DocumentProcessingException("DOCLING_UNAVAILABLE", $"Docling returned HTTP {(int)response.StatusCode}.");
        }

        try
        {
            return JsonDocument.Parse(content);
        }
        catch (JsonException)
        {
            throw new DocumentProcessingException("DOCLING_INVALID_RESPONSE", "Docling returned malformed JSON.");
        }
    }

    private void AddApiKey(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Add("X-Api-Key", _options.ApiKey);
        }
    }

    private bool RequiresForcedOcr(DocumentProcessingResult result)
    {
        var pages = Math.Max(1, result.PageCount);
        var charactersPerPage = Math.Max(result.Markdown.Length, result.PlainText.Length) / pages;
        return string.IsNullOrWhiteSpace(result.Markdown) ||
               charactersPerPage < _options.MinimumCharactersPerPage ||
               result.Warnings.Count > 0;
    }

    private static double CalculateQuality(string markdown, string plainText, int pageCount)
    {
        var characters = Math.Max(markdown.Trim().Length, plainText.Trim().Length);
        var pages = Math.Max(1, pageCount);
        return Math.Clamp(characters / (pages * 500d), 0d, 1d);
    }

    private static string GetError(JsonElement root)
    {
        return GetStringValues(root, "errors").FirstOrDefault() ?? "Docling could not convert the document.";
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString();
                }

                var nested = GetString(property.Value, propertyName);
                if (nested != null)
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = GetString(item, propertyName);
                if (nested != null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static int GetArrayLength(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.Array)
                {
                    return property.Value.GetArrayLength();
                }

                var nested = GetArrayLength(property.Value, propertyName);
                if (nested > 0)
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = GetArrayLength(item, propertyName);
                if (nested > 0)
                {
                    return nested;
                }
            }
        }

        return 0;
    }

    private static IReadOnlyList<string> GetStringValues(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.Array)
                {
                    return property.Value.EnumerateArray()
                        .Select(value => value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString())
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Cast<string>()
                        .ToArray();
                }

                var nested = GetStringValues(property.Value, propertyName);
                if (nested.Count > 0)
                {
                    return nested;
                }
            }
        }

        return Array.Empty<string>();
    }

    private sealed class NonDisposingStream : Stream
    {
        private readonly Stream _inner;

        public NonDisposingStream(Stream inner) => _inner = inner;

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }
        public override void Flush() => _inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => _inner.Read(buffer);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _inner.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer) => _inner.Write(buffer);
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => _inner.WriteAsync(buffer, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            // The caller owns the stream and needs it for the force-OCR retry.
        }
    }
}
