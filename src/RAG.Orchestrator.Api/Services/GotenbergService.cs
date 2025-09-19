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
        // LibreOffice (dokumenty biurowe) - pełna lista z dokumentacji
        ".123", ".602", ".abw", ".bib", ".bmp", ".cdr", ".cgm", ".cmx", ".csv", ".cwk", ".dbf",
        ".dif", ".doc", ".docm", ".docx", ".dot", ".dotm", ".dotx", ".dxf", ".emf", ".eps", ".epub",
        ".fodg", ".fodp", ".fods", ".fodt", ".fopd", ".gif", ".htm", ".html", ".hwp", ".jpeg", ".jpg",
        ".key", ".ltx", ".lwp", ".mcw", ".met", ".mml", ".mw", ".numbers", ".odd", ".odg", ".odm",
        ".odp", ".ods", ".odt", ".otg", ".oth", ".otp", ".ots", ".ott", ".pages", ".pbm", ".pcd",
        ".pct", ".pcx", ".pdb", ".pdf", ".pgm", ".pict", ".plt", ".png", ".pot", ".potm", ".potx",
        ".ppm", ".pps", ".ppsm", ".ppsx", ".ppt", ".pptm", ".pptx", ".psd", ".pub", ".pwz", ".pxl",
        ".ras", ".rtf", ".sda", ".sdc", ".sdd", ".sdp", ".sdw", ".sgl", ".sldm", ".sldx", ".slk",
        ".stc", ".std", ".sti", ".stw", ".svg", ".svm", ".swf", ".sxc", ".sxd", ".sxg", ".sxi",
        ".sxm", ".sxw", ".tif", ".tiff", ".txt", ".uof", ".uop", ".uos", ".uot", ".vdx", ".vor",
        ".vsd", ".vsdm", ".vsdx", ".vst", ".vstm", ".vstx", ".wb2", ".wk1", ".wks", ".wma", ".wmf",
        ".wmv", ".wpd", ".wpg", ".wps", ".xbm", ".xcf", ".xls", ".xlsb", ".xlsm", ".xlsx", ".xlt",
        ".xltm", ".xltx", ".xlw", ".xml", ".xpm", ".zw",

        // Chromium (HTML, Markdown)
        ".html", ".htm", ".md", ".markdown",

        // Obrazy (obsługiwane przez LibreOffice)
        ".bmp", ".gif", ".jpeg", ".jpg", ".png", ".svg", ".tiff", ".webp"
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

            var endpoint = GetConversionEndpoint(fileExtension);

            // Special handling for different file types
            if (endpoint.Contains("/chromium/convert/markdown"))
            {
                // For Markdown files, we need to provide both index.html and the markdown file
                // Create a simple HTML wrapper for the markdown content
                var markdownFileName = Path.GetFileNameWithoutExtension(fileName) + ".md";
                var indexHtmlContent = $@"<!DOCTYPE html>
                    <html lang=""en"">
                    <head>
                        <meta charset=""utf-8"">
                        <title>Markdown Document</title>
                    </head>
                    <body>
                        {{{{ toHTML ""{markdownFileName}"" }}}}
                    </body>
                    </html>";

                // Add the HTML wrapper
                var indexHtmlStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(indexHtmlContent));
                var indexHtmlContentStream = new StreamContent(indexHtmlStream);
                indexHtmlContentStream.Headers.ContentType = new MediaTypeHeaderValue("text/html");
                content.Add(indexHtmlContentStream, "index.html", "index.html");

                // Add the markdown file
                var markdownContent = new StreamContent(fileStream);
                markdownContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(fileExtension));
                content.Add(markdownContent, "files", fileName);
            }
            else
            {
                // Standard handling for other file types
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(fileExtension));

                // For Chromium HTML endpoint, the file should be named "index.html"
                if (endpoint.Contains("/chromium/convert/html"))
                {
                    content.Add(fileContent, "index.html", "index.html");
                }
                else
                {
                    // For LibreOffice and other endpoints, use the original filename
                    content.Add(fileContent, "files", fileName);
                }
            }

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
        // Normalize extension to lowercase
        var extension = fileExtension.ToLowerInvariant();

        // Chromium routes - for web-based formats
        var chromiumExtensions = new[] { ".html", ".htm", ".md", ".markdown" };
        if (chromiumExtensions.Contains(extension))
        {
            // For HTML files, use the HTML conversion endpoint
            if (extension == ".html" || extension == ".htm")
            {
                return "/forms/chromium/convert/html";
            }
            // For Markdown files, use the Markdown conversion endpoint
            else if (extension == ".md" || extension == ".markdown")
            {
                return "/forms/chromium/convert/markdown";
            }
        }

        // LibreOffice routes - for office documents
        var officeExtensions = new[]
        {
            ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".odt", ".ods", ".odp", ".rtf", ".txt", ".csv",
            ".bmp", ".gif", ".jpeg", ".jpg", ".png", ".svg",
            ".tiff", ".webp", ".epub"
        };
        if (officeExtensions.Contains(extension))
        {
            return "/forms/libreoffice/convert";
        }

        // PDF files - use PDF engines for PDF/A conversion or just return as-is
        if (extension == ".pdf")
        {
            // For PDF files, we might want to convert to PDF/A format
            // For now, we'll use LibreOffice as fallback since it can handle PDFs
            return "/forms/libreoffice/convert";
        }

        // Default fallback - try LibreOffice for any other format
        _logger.LogWarning("Unknown file extension {Extension}, using LibreOffice as fallback", extension);
        return "/forms/libreoffice/convert";
    }

    private string GetMimeType(string fileExtension)
    {
        return fileExtension.ToLowerInvariant() switch
        {
            // Microsoft Office
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".docm" => "application/vnd.ms-word.document.macroEnabled.12",
            ".dot" => "application/msword",
            ".dotx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.template",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xlsm" => "application/vnd.ms-excel.sheet.macroEnabled.12",
            ".xlt" => "application/vnd.ms-excel",
            ".xltx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.template",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".pptm" => "application/vnd.ms-powerpoint.presentation.macroEnabled.12",
            ".pot" => "application/vnd.ms-powerpoint",
            ".potx" => "application/vnd.openxmlformats-officedocument.presentationml.template",

            // OpenDocument
            ".odt" => "application/vnd.oasis.opendocument.text",
            ".ods" => "application/vnd.oasis.opendocument.spreadsheet",
            ".odp" => "application/vnd.oasis.opendocument.presentation",
            ".odg" => "application/vnd.oasis.opendocument.graphics",
            ".odf" => "application/vnd.oasis.opendocument.formula",

            // Text formats
            ".rtf" => "application/rtf",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".html" => "text/html",
            ".htm" => "text/html",
            ".xml" => "application/xml",
            ".md" => "text/markdown",
            ".markdown" => "text/markdown",

            // PDF
            ".pdf" => "application/pdf",

            // Images
            ".bmp" => "image/bmp",
            ".gif" => "image/gif",
            ".jpeg" => "image/jpeg",
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".svg" => "image/svg+xml",
            ".tiff" => "image/tiff",
            ".tif" => "image/tiff",
            ".webp" => "image/webp",

            // Other formats
            ".epub" => "application/epub+zip",
            ".zip" => "application/zip",

            _ => "application/octet-stream"
        };
    }
}