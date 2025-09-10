using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;

namespace RAG.Collector.ContentExtractors;

/// <summary>
/// Content extractor for Microsoft Office documents (.docx, .xlsx, .pptx)
/// </summary>
public class OfficeDocumentExtractor : IContentExtractor
{
    private readonly ILogger<OfficeDocumentExtractor> _logger;

    private static readonly string[] _supportedExtensions = { ".docx", ".xlsx", ".pptx" };

    public OfficeDocumentExtractor(ILogger<OfficeDocumentExtractor> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IEnumerable<string> SupportedExtensions => _supportedExtensions;

    /// <inheritdoc />
    public bool CanExtract(string extension)
    {
        return _supportedExtensions.Contains(extension.ToLowerInvariant());
    }

    /// <inheritdoc />
    public async Task<ContentExtractionResult> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Extracting content from Office document: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                return ContentExtractionResult.Failure($"File not found: {filePath}");
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            return await Task.Run(() => extension switch
            {
                ".docx" => ExtractWordDocument(filePath),
                ".xlsx" => ExtractExcelDocument(filePath),
                ".pptx" => ExtractPowerPointDocument(filePath),
                _ => ContentExtractionResult.Failure($"Unsupported Office document type: {extension}")
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract content from Office document: {FilePath}", filePath);
            return ContentExtractionResult.Failure($"Error reading Office document: {ex.Message}");
        }
    }

    /// <summary>
    /// Extracts content from Word document (.docx)
    /// </summary>
    private ContentExtractionResult ExtractWordDocument(string filePath)
    {
        using var document = WordprocessingDocument.Open(filePath, false);

        var metadata = ExtractBasicMetadata(filePath);

        // Extract document properties from package
        ExtractPackageProperties(document, metadata);

        // Extract text content
        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null)
        {
            return ContentExtractionResult.Failure("Document body not found");
        }

        var contentBuilder = new StringBuilder();
        ExtractTextFromBody(body, contentBuilder);

        var content = contentBuilder.ToString();
        AddContentStatistics(content, metadata);

        _logger.LogDebug("Extracted {CharCount} characters from Word document {FilePath}", content.Length, filePath);

        return ContentExtractionResult.Success(content, metadata);
    }

    /// <summary>
    /// Extracts content from Excel document (.xlsx)
    /// </summary>
    private ContentExtractionResult ExtractExcelDocument(string filePath)
    {
        using var document = SpreadsheetDocument.Open(filePath, false);

        var metadata = ExtractBasicMetadata(filePath);

        // Extract document properties from package
        ExtractPackageProperties(document, metadata);

        var workbookPart = document.WorkbookPart;
        if (workbookPart == null)
        {
            return ContentExtractionResult.Failure("Workbook not found");
        }

        var contentBuilder = new StringBuilder();
        var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;
        var worksheetCount = 0;

        // Extract content from each worksheet
        foreach (var worksheetPart in workbookPart.WorksheetParts)
        {
            worksheetCount++;
            var worksheet = worksheetPart.Worksheet;
            var sheetData = worksheet.GetFirstChild<SheetData>();

            if (sheetData != null)
            {
                contentBuilder.AppendLine($"\n--- Sheet {worksheetCount} ---");
                ExtractTextFromSheet(sheetData, sharedStringTable, contentBuilder);
            }
        }

        var content = contentBuilder.ToString();
        metadata["WorksheetCount"] = worksheetCount.ToString();
        AddContentStatistics(content, metadata);

        _logger.LogDebug("Extracted {CharCount} characters from {SheetCount} sheets in Excel document {FilePath}",
            content.Length, worksheetCount, filePath);

        return ContentExtractionResult.Success(content, metadata);
    }

    /// <summary>
    /// Extracts content from PowerPoint document (.pptx)
    /// </summary>
    private ContentExtractionResult ExtractPowerPointDocument(string filePath)
    {
        using var document = PresentationDocument.Open(filePath, false);

        var metadata = ExtractBasicMetadata(filePath);

        // Extract document properties from package
        ExtractPackageProperties(document, metadata);

        var presentationPart = document.PresentationPart;
        if (presentationPart == null)
        {
            return ContentExtractionResult.Failure("Presentation not found");
        }

        var contentBuilder = new StringBuilder();
        var slideCount = 0;

        // Extract content from each slide
        foreach (var slidePart in presentationPart.SlideParts)
        {
            slideCount++;
            contentBuilder.AppendLine($"\n--- Slide {slideCount} ---");

            var slide = slidePart.Slide;
            ExtractTextFromSlide(slide, contentBuilder);
        }

        var content = contentBuilder.ToString();
        metadata["SlideCount"] = slideCount.ToString();
        AddContentStatistics(content, metadata);

        _logger.LogDebug("Extracted {CharCount} characters from {SlideCount} slides in PowerPoint document {FilePath}",
            content.Length, slideCount, filePath);

        return ContentExtractionResult.Success(content, metadata, slideCount);
    }

    /// <summary>
    /// Extracts basic file metadata
    /// </summary>
    private static Dictionary<string, string> ExtractBasicMetadata(string filePath)
    {
        var metadata = new Dictionary<string, string>();
        var fileInfo = new FileInfo(filePath);

        metadata["FileName"] = fileInfo.Name;
        metadata["FileSize"] = fileInfo.Length.ToString();
        metadata["LastModified"] = fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss UTC");

        return metadata;
    }

    /// <summary>
    /// Extracts package properties from Office document
    /// </summary>
    private static void ExtractPackageProperties<T>(T document, Dictionary<string, string> metadata) where T : OpenXmlPackage
    {
        try
        {
            if (document.PackageProperties != null)
            {
                var props = document.PackageProperties;

                if (!string.IsNullOrEmpty(props.Title)) metadata["Title"] = props.Title;
                if (!string.IsNullOrEmpty(props.Creator)) metadata["Author"] = props.Creator;
                if (!string.IsNullOrEmpty(props.Subject)) metadata["Subject"] = props.Subject;
                if (!string.IsNullOrEmpty(props.Description)) metadata["Description"] = props.Description;
                if (!string.IsNullOrEmpty(props.Category)) metadata["Category"] = props.Category;
                if (!string.IsNullOrEmpty(props.Keywords)) metadata["Keywords"] = props.Keywords;
                if (props.Created.HasValue) metadata["CreationDate"] = props.Created.Value.ToString("yyyy-MM-dd HH:mm:ss");
                if (props.Modified.HasValue) metadata["ModifiedDate"] = props.Modified.Value.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
        catch (Exception)
        {
            // Ignore metadata extraction errors - content extraction should continue
        }
    }

    /// <summary>
    /// Extracts text from Word document body
    /// </summary>
    private static void ExtractTextFromBody(Body body, StringBuilder contentBuilder)
    {
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            var paragraphText = paragraph.InnerText;
            if (!string.IsNullOrWhiteSpace(paragraphText))
            {
                contentBuilder.AppendLine(paragraphText);
            }
        }
    }

    /// <summary>
    /// Extracts text from Excel sheet
    /// </summary>
    private static void ExtractTextFromSheet(SheetData sheetData, SharedStringTable? sharedStringTable, StringBuilder contentBuilder)
    {
        foreach (var row in sheetData.Descendants<Row>())
        {
            var rowValues = new List<string>();

            foreach (var cell in row.Descendants<Cell>())
            {
                var cellValue = GetCellValue(cell, sharedStringTable);
                if (!string.IsNullOrWhiteSpace(cellValue))
                {
                    rowValues.Add(cellValue);
                }
            }

            if (rowValues.Count > 0)
            {
                contentBuilder.AppendLine(string.Join("\t", rowValues));
            }
        }
    }

    /// <summary>
    /// Extracts text from PowerPoint slide
    /// </summary>
    private static void ExtractTextFromSlide(Slide slide, StringBuilder contentBuilder)
    {
        foreach (var textBody in slide.Descendants<DocumentFormat.OpenXml.Drawing.TextBody>())
        {
            var slideText = textBody.InnerText;
            if (!string.IsNullOrWhiteSpace(slideText))
            {
                contentBuilder.AppendLine(slideText);
            }
        }
    }

    /// <summary>
    /// Gets the value of an Excel cell
    /// </summary>
    private static string GetCellValue(Cell cell, SharedStringTable? sharedStringTable)
    {
        var cellValue = cell.CellValue?.Text;
        if (string.IsNullOrEmpty(cellValue))
            return string.Empty;

        // Handle shared strings
        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
        {
            if (sharedStringTable != null && int.TryParse(cellValue, out int ssid))
            {
                return sharedStringTable.ElementAt(ssid).InnerText;
            }
        }

        return cellValue;
    }

    /// <summary>
    /// Adds content statistics to metadata
    /// </summary>
    private static void AddContentStatistics(string content, Dictionary<string, string> metadata)
    {
        metadata["CharacterCount"] = content.Length.ToString();
        metadata["WordCount"] = EstimateWordCount(content).ToString();
        metadata["LineCount"] = content.Split('\n').Length.ToString();
    }

    /// <summary>
    /// Estimates word count in text content
    /// </summary>
    private static int EstimateWordCount(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        return content.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
