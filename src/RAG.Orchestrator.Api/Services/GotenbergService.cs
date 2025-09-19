using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using RAG.Abstractions.Conversion;
using RAG.Orchestrator.Api.Services;
using RAG.Orchestrator.Api.Models.Configuration;

public class GotenbergService : IGotenbergService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GotenbergService> _logger;
    private readonly GotenbergConfig _config;

    // Lista obsługiwanych rozszerzeń plików przez Gotenberg
    private static readonly HashSet<string> _supportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // LibreOffice (dokumenty biurowe)
        ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt", ".ods", ".odp",
        ".rtf", ".txt", ".csv", ".html", ".htm", ".xml", ".pdf",

        // Chromium (HTML, Markdown)
        ".html", ".htm", ".md", ".markdown",

        // Obrazy
        ".bmp", ".gif", ".jpeg", ".jpg", ".png", ".svg", ".tiff", ".webp",

        // Inne
        ".epub"
    };

    public GotenbergService(HttpClient httpClient, IOptions<GotenbergConfig> config, ILogger<GotenbergService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;

        // Konfiguracja HttpClient na podstawie ustawień
        _httpClient.BaseAddress = new Uri(_config.Url);
        _httpClient.Timeout = TimeSpan.FromMinutes(_config.TimeoutMinutes);

        _logger.LogInformation("GotenbergService configured with URL: {Url}, Timeout: {Timeout}min",
            _config.Url, _config.TimeoutMinutes);
    }

    public Task<bool> CanConvertAsync(string fileExtension)
    {
        if (string.IsNullOrEmpty(fileExtension))
            return Task.FromResult(false);

        // Usuń kropkę jeśli istnieje
        if (fileExtension.StartsWith('.'))
            fileExtension = fileExtension[1..];

        var extension = $".{fileExtension.ToLowerInvariant()}";
        return Task.FromResult(_supportedExtensions.Contains(extension));
    }

    public async Task<Stream?> ConvertToPdfAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (fileStream == null || string.IsNullOrWhiteSpace(fileName))
            {
                _logger.LogWarning("Invalid parameters for PDF conversion");
                return null;
            }

            var fileExtension = Path.GetExtension(fileName);
            if (!await CanConvertAsync(fileExtension))
            {
                _logger.LogInformation("File extension {Extension} is not supported for PDF conversion", fileExtension);
                return null;
            }

            // Przygotuj multipart form data
            using var content = new MultipartFormDataContent();

            // Dodaj plik do requestu
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(fileExtension));

            content.Add(fileContent, "files", fileName);

            // Wybierz odpowiedni endpoint na podstawie typu pliku
            var endpoint = GetConversionEndpoint(fileExtension);

            _logger.LogInformation("Converting file {FileName} to PDF using endpoint {Endpoint}", fileName, endpoint);

            // Wyślij request do Gotenberg
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gotenberg conversion failed with status {StatusCode} for file {FileName}",
                    response.StatusCode, fileName);
                return null;
            }

            // Zwróć stream z przekonwertowanym PDF
            var pdfStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            _logger.LogInformation("Successfully converted file {FileName} to PDF", fileName);

            return pdfStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting file {FileName} to PDF", fileName);
            return null;
        }
    }

    private string GetConversionEndpoint(string fileExtension)
    {
        // Dla dokumentów biurowych użyj LibreOffice
        var officeExtensions = new[] { ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt", ".ods", ".odp", ".rtf", ".txt", ".csv" };
        if (officeExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
        {
            return "/forms/libreoffice/convert";
        }

        // Dla HTML i Markdown użyj Chromium
        var chromiumExtensions = new[] { ".html", ".htm", ".md", ".markdown" };
        if (chromiumExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
        {
            return "/forms/chromium/convert/html";
        }

        // Domyślnie LibreOffice dla innych typów
        return "/forms/libreoffice/convert";
    }

    private string GetMimeType(string fileExtension)
    {
        return fileExtension.ToLowerInvariant() switch
        {
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".odt" => "application/vnd.oasis.opendocument.text",
            ".ods" => "application/vnd.oasis.opendocument.spreadsheet",
            ".odp" => "application/vnd.oasis.opendocument.presentation",
            ".rtf" => "application/rtf",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".html" => "text/html",
            ".htm" => "text/html",
            ".md" => "text/markdown",
            ".markdown" => "text/markdown",
            ".pdf" => "application/pdf",
            ".epub" => "application/epub+zip",
            ".bmp" => "image/bmp",
            ".gif" => "image/gif",
            ".jpeg" => "image/jpeg",
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".svg" => "image/svg+xml",
            ".tiff" => "image/tiff",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}