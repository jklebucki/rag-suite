using Microsoft.Extensions.Options;
using RAG.DocumentProcessing.Abstractions;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RAG.DocumentProcessing.Client;

public sealed class DocumentProcessingClient : IDocumentProcessingClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public DocumentProcessingClient(HttpClient httpClient, IOptions<DocumentProcessingClientOptions> options)
    {
        _httpClient = httpClient;
        var settings = options.Value;
        _httpClient.BaseAddress = new Uri(settings.Endpoint, UriKind.Absolute);
        _httpClient.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", settings.ApiKey);
        }
    }

    public async Task<DocumentJobAccepted> SubmitAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        try
        {
            using var form = new MultipartFormDataContent();
            using var fileContent = new StreamContent(content);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            form.Add(fileContent, "file", fileName);
            using var response = await _httpClient.PostAsync("api/v1/jobs", form, cancellationToken);
            return await ReadRequiredAsync<DocumentJobAccepted>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new DocumentProcessingException("DOCUMENT_PROCESSING_UNAVAILABLE", ex.Message);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DocumentProcessingException("DOCUMENT_PROCESSING_TIMEOUT", "Document processing did not respond before the configured timeout.");
        }
    }

    public async Task<DocumentJobStatus> GetStatusAsync(string jobId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"api/v1/jobs/{Uri.EscapeDataString(jobId)}", cancellationToken);
            return await ReadRequiredAsync<DocumentJobStatus>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new DocumentProcessingException("DOCUMENT_PROCESSING_UNAVAILABLE", ex.Message);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DocumentProcessingException("DOCUMENT_PROCESSING_TIMEOUT", "Document processing did not respond before the configured timeout.");
        }
    }

    public async Task<DocumentProcessingResult> GetResultAsync(string jobId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"api/v1/jobs/{Uri.EscapeDataString(jobId)}/result", cancellationToken);
            return await ReadRequiredAsync<DocumentProcessingResult>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new DocumentProcessingException("DOCUMENT_PROCESSING_UNAVAILABLE", ex.Message);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DocumentProcessingException("DOCUMENT_PROCESSING_TIMEOUT", "Document processing did not respond before the configured timeout.");
        }
    }

    private static async Task<T> ReadRequiredAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new DocumentProcessingException("DOCUMENT_PROCESSING_UNAVAILABLE", $"Document processing returned HTTP {(int)response.StatusCode}.");
        }

        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        return value ?? throw new DocumentProcessingException("DOCUMENT_PROCESSING_INVALID_RESPONSE", "Document processing returned an empty response.");
    }
}
