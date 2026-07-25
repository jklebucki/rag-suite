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
        var primaryLanguages = _options.GetOcrLanguages();
        var result = await ProcessOnceAsync(
            request,
            forceOcr: request.ForceOcr,
            _options.OcrPreset,
            primaryLanguages,
            cancellationToken);
        if (!request.ForceOcr && RequiresForcedOcr(result))
        {
            ResetContent(request);
            _logger.LogInformation(
                "Retrying document {FileName} with force_ocr=true and OCR preset {OcrPreset}",
                request.Document.FileName,
                _options.OcrPreset);
            var retried = await ProcessOnceAsync(
                request,
                forceOcr: true,
                _options.OcrPreset,
                primaryLanguages,
                cancellationToken);
            result = SelectHigherQualityResult(result, retried) with
            {
                Warnings = MergeWarnings(
                    result.Warnings,
                    retried.Warnings,
                    "The initial extraction was retried with force_ocr=true because its text quality was low.")
            };
        }

        if (!_options.EnableTableFallback ||
            !MarkdownTableRescuer.NeedsFallback(result.Markdown, _options.MinimumTableCompleteness))
        {
            return result;
        }

        try
        {
            ResetContent(request);
            _logger.LogInformation(
                "Retrying tables in document {FileName} with OCR preset {OcrPreset}",
                request.Document.FileName,
                _options.TableFallbackOcrPreset);
            var fallback = await ProcessOnceAsync(
                request,
                forceOcr: request.ForceOcr,
                _options.TableFallbackOcrPreset,
                _options.GetTableFallbackOcrLanguages(),
                cancellationToken);
            var mergedMarkdown = MarkdownTableRescuer.MergeBetterTables(
                result.Markdown,
                fallback.Markdown,
                _options.MinimumTableCompleteness,
                out var replacementCount);
            if (replacementCount == 0)
            {
                return result;
            }

            var mergedPlainText = MarkdownTableRescuer.MergeBetterTables(
                result.PlainText,
                fallback.PlainText,
                _options.MinimumTableCompleteness,
                out _);
            return result with
            {
                Markdown = mergedMarkdown,
                PlainText = mergedPlainText,
                QualityScore = OcrTextQualityEvaluator.Calculate(mergedMarkdown, mergedPlainText, result.PageCount),
                Warnings = MergeWarnings(
                    result.Warnings,
                    fallback.Warnings,
                    $"Replaced {replacementCount} incomplete Markdown table(s) using OCR preset '{_options.TableFallbackOcrPreset}'.")
            };
        }
        catch (DocumentProcessingException exception)
        {
            _logger.LogWarning(
                exception,
                "Table fallback failed for document {FileName}; returning the primary OCR result",
                request.Document.FileName);
            return result with
            {
                Warnings = MergeWarnings(
                    result.Warnings,
                    Array.Empty<string>(),
                    $"The table fallback preset '{_options.TableFallbackOcrPreset}' was unavailable.")
            };
        }
    }

    private async Task<DocumentProcessingResult> ProcessOnceAsync(
        DocumentProcessingRequest request,
        bool forceOcr,
        string ocrPreset,
        IReadOnlyList<string> ocrLanguages,
        CancellationToken cancellationToken)
    {
        var taskId = await SubmitAsync(request, forceOcr, ocrPreset, ocrLanguages, cancellationToken);
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
        var qualityScore = OcrTextQualityEvaluator.Calculate(markdown, plainText, pageCount);
        return new DocumentProcessingResult(
            markdown,
            plainText,
            pageCount,
            OcrUsed: true,
            Provider: Name,
            qualityScore,
            warnings);
    }

    private async Task<string> SubmitAsync(
        DocumentProcessingRequest request,
        bool forceOcr,
        string ocrPreset,
        IReadOnlyList<string> ocrLanguages,
        CancellationToken cancellationToken)
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
        form.Add(new StringContent(ocrPreset), "ocr_preset");
        foreach (var ocrLanguage in ocrLanguages)
        {
            form.Add(new StringContent(ocrLanguage), "ocr_lang");
        }

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
               result.QualityScore < _options.MinimumTextQuality ||
               result.Warnings.Count > 0;
    }

    private static DocumentProcessingResult SelectHigherQualityResult(
        DocumentProcessingResult initial,
        DocumentProcessingResult retried)
    {
        return retried.QualityScore >= initial.QualityScore ? retried : initial;
    }

    private static IReadOnlyList<string> MergeWarnings(
        IReadOnlyList<string> primary,
        IReadOnlyList<string> secondary,
        string additionalWarning)
    {
        return primary.Concat(secondary)
            .Append(additionalWarning)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static void ResetContent(DocumentProcessingRequest request)
    {
        if (!request.Content.CanSeek)
        {
            throw new DocumentProcessingException(
                "DOCUMENT_STREAM_NOT_SEEKABLE",
                "The document stream cannot be reset for an OCR retry.");
        }

        request.Content.Position = 0;
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
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value.ValueKind switch
                    {
                        JsonValueKind.Array => property.Value.GetArrayLength(),
                        JsonValueKind.Object => property.Value.EnumerateObject().Count(),
                        _ => 0
                    };
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
