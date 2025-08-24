using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Logging;
using RAG.Connectors.Files.Interfaces;
using RAG.Connectors.Files.Models;
using System.Text;

namespace RAG.Connectors.Files.Parsers;

public class ExcelDocumentParser : IDocumentParser
{
    private readonly ILogger<ExcelDocumentParser> _logger;

    public string[] SupportedExtensions => new[] { ".xlsx", ".xls" };

    public ExcelDocumentParser(ILogger<ExcelDocumentParser> logger)
    {
        _logger = logger;
    }

    public bool CanParse(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public async Task<DocumentContent> ParseAsync(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var content = await ExtractTextFromExcelAsync(filePath);
            
            return new DocumentContent
            {
                Id = Guid.NewGuid().ToString(),
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                Content = content,
                FileType = "Excel Spreadsheet",
                CreatedAt = fileInfo.CreationTimeUtc,
                ModifiedAt = fileInfo.LastWriteTimeUtc,
                FileSize = fileInfo.Length,
                Metadata = new Dictionary<string, object>
                {
                    ["parser"] = "ExcelDocumentParser",
                    ["encoding"] = "UTF-8"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Excel document: {FilePath}", filePath);
            throw;
        }
    }

    private async Task<string> ExtractTextFromExcelAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            var text = new StringBuilder();

            using var spreadsheetDocument = SpreadsheetDocument.Open(filePath, false);
            var workbookPart = spreadsheetDocument.WorkbookPart;
            var sharedStringTablePart = workbookPart?.SharedStringTablePart;
            var sharedStringTable = sharedStringTablePart?.SharedStringTable;

            if (workbookPart?.Workbook.Sheets != null)
            {
                foreach (Sheet sheet in workbookPart.Workbook.Sheets)
                {
                    text.AppendLine($"=== {sheet.Name} ===");
                    
                    var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
                    var worksheet = worksheetPart.Worksheet;
                    var sheetData = worksheet.GetFirstChild<SheetData>();

                    if (sheetData != null)
                    {
                        foreach (Row row in sheetData.Elements<Row>())
                        {
                            var rowText = new List<string>();
                            foreach (Cell cell in row.Elements<Cell>())
                            {
                                var cellValue = GetCellValue(cell, sharedStringTable);
                                rowText.Add(cellValue);
                            }
                            if (rowText.Any(x => !string.IsNullOrWhiteSpace(x)))
                            {
                                text.AppendLine(string.Join(" | ", rowText));
                            }
                        }
                    }
                    text.AppendLine();
                }
            }

            return text.ToString();
        });
    }

    private static string GetCellValue(Cell cell, SharedStringTable? sharedStringTable)
    {
        if (cell.CellValue == null) return string.Empty;

        var value = cell.CellValue.InnerText;

        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
        {
            if (sharedStringTable != null && int.TryParse(value, out var id))
            {
                return sharedStringTable.Elements<SharedStringItem>().ElementAt(id).InnerText;
            }
        }

        return value;
    }
}
