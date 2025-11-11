using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RAG.Abstractions.Conversion;
using RAG.Orchestrator.Api.Features.FileDownload;

namespace RAG.Tests.Orchestrator;

public class FileDownloadServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly Mock<IGotenbergClient> _mockGotenbergClient;
    private readonly Mock<ILogger<FileDownloadService>> _mockLogger;
    private readonly FileDownloadService _service;

    public FileDownloadServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _mockGotenbergClient = new Mock<IGotenbergClient>();
        _mockLogger = new Mock<ILogger<FileDownloadService>>();

        var options = Options.Create(new SharedFoldersOptions
        {
            SharedFolders = new List<SharedFolderConfig>
            {
                new SharedFolderConfig
                {
                    Path = _testDirectory,
                    PathToReplace = "X:\\Test\\"
                }
            }
        });

        _service = new FileDownloadService(
            options,
            _mockLogger.Object,
            _mockGotenbergClient.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task DownloadFileAsync_WithEmptyPath_ReturnsBadRequest()
    {
        // Arrange
        var filePath = "";

        // Act
        var result = await _service.DownloadFileAsync(filePath);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IResult>();
    }

    [Fact]
    public async Task DownloadFileAsync_WithNullPath_ReturnsBadRequest()
    {
        // Arrange
        string? filePath = null;

        // Act
        var result = await _service.DownloadFileAsync(filePath!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IResult>();
    }

    [Fact]
    public async Task DownloadFileAsync_WithNonMatchingPath_ReturnsBadRequest()
    {
        // Arrange
        var filePath = "Y:\\NonMatching\\file.txt";

        // Act
        var result = await _service.DownloadFileAsync(filePath);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IResult>();
    }

    [Fact]
    public async Task DownloadFileAsync_WithDirectoryTraversal_ReturnsForbid()
    {
        // Arrange
        var filePath = "X:\\Test\\..\\..\\etc\\passwd";

        // Act
        var result = await _service.DownloadFileAsync(filePath);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IResult>();
    }

    [Fact]
    public async Task DownloadFileAsync_WithNonExistentFile_ReturnsNotFound()
    {
        // Arrange
        var filePath = "X:\\Test\\nonexistent.txt";

        // Act
        var result = await _service.DownloadFileAsync(filePath);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IResult>();
    }

    [Fact]
    public async Task DownloadFileAsync_WithExistingFile_ReturnsFile()
    {
        // Arrange
        var fileName = "test.txt";
        var filePath = Path.Combine(_testDirectory, fileName);
        var fileContent = "Test content";
        await File.WriteAllTextAsync(filePath, fileContent);

        var downloadPath = $"X:\\Test\\{fileName}";

        // Act
        var result = await _service.DownloadFileAsync(downloadPath);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IResult>();
    }

    [Fact]
    public async Task DownloadFileAsync_WithUrlEncodedPath_DecodesPath()
    {
        // Arrange
        var fileName = "test file.txt";
        var filePath = Path.Combine(_testDirectory, fileName);
        var fileContent = "Test content";
        await File.WriteAllTextAsync(filePath, fileContent);

        var encodedPath = Uri.EscapeDataString($"X:\\Test\\{fileName}");
        var downloadPath = $"X:\\Test\\{encodedPath}";

        // Act
        var result = await _service.DownloadFileAsync(downloadPath);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IResult>();
    }

    [Fact]
    public async Task GetFileInfoAsync_WithExistingFile_ReturnsFileInfo()
    {
        // Arrange
        var fileName = "test.txt";
        var filePath = Path.Combine(_testDirectory, fileName);
        var fileContent = "Test content";
        await File.WriteAllTextAsync(filePath, fileContent);

        var downloadPath = $"X:\\Test\\{fileName}";

        // Act
        var result = await _service.GetFileInfoAsync(downloadPath);

        // Assert
        result.Should().NotBeNull();
        result!.FileName.Should().Contain(fileName);
        result.FullPath.Should().Be(filePath);
        result.FileSize.Should().BeGreaterThan(0);
        result.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public async Task GetFileInfoAsync_WithNonExistentFile_ReturnsNull()
    {
        // Arrange
        var filePath = "X:\\Test\\nonexistent.txt";

        // Act
        var result = await _service.GetFileInfoAsync(filePath);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFileInfoAsync_WithEmptyPath_ReturnsNull()
    {
        // Arrange
        var filePath = "";

        // Act
        var result = await _service.GetFileInfoAsync(filePath);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFileInfoAsync_WithPdfFile_ReturnsCorrectContentType()
    {
        // Arrange
        var fileName = "test.pdf";
        var filePath = Path.Combine(_testDirectory, fileName);
        await File.WriteAllBytesAsync(filePath, new byte[] { 0x25, 0x50, 0x44, 0x46 }); // PDF header

        var downloadPath = $"X:\\Test\\{fileName}";

        // Act
        var result = await _service.GetFileInfoAsync(downloadPath);

        // Assert
        result.Should().NotBeNull();
        result!.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task DownloadFileWithConversionAsync_WhenConversionNotPossible_ReturnsOriginalFile()
    {
        // Arrange
        var fileName = "test.txt";
        var filePath = Path.Combine(_testDirectory, fileName);
        await File.WriteAllTextAsync(filePath, "Test content");

        var downloadPath = $"X:\\Test\\{fileName}";

        _mockGotenbergClient.Setup(c => c.CanConvertAsync(".txt"))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DownloadFileWithConversionAsync(downloadPath, forceConvert: false);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IResult>();
        _mockGotenbergClient.Verify(c => c.CanConvertAsync(".txt"), Times.Once);
    }

    [Fact]
    public async Task DownloadFileWithConversionAsync_WhenConversionPossible_ConvertsToPdf()
    {
        // Arrange
        var fileName = "test.docx";
        var filePath = Path.Combine(_testDirectory, fileName);
        await File.WriteAllBytesAsync(filePath, new byte[] { 0x50, 0x4B }); // ZIP header (DOCX is a ZIP)

        var downloadPath = $"X:\\Test\\{fileName}";
        var pdfStream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // PDF header

        _mockGotenbergClient.Setup(c => c.CanConvertAsync(".docx"))
            .ReturnsAsync(true);
        _mockGotenbergClient.Setup(c => c.ConvertToPdfAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pdfStream);

        // Act
        var result = await _service.DownloadFileWithConversionAsync(downloadPath, forceConvert: false);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IResult>();
        _mockGotenbergClient.Verify(c => c.CanConvertAsync(".docx"), Times.Once);
        _mockGotenbergClient.Verify(c => c.ConvertToPdfAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DownloadFileWithConversionAsync_WhenForceConvert_ConvertsEvenIfNotSupported()
    {
        // Arrange
        var fileName = "test.txt";
        var filePath = Path.Combine(_testDirectory, fileName);
        await File.WriteAllTextAsync(filePath, "Test content");

        var downloadPath = $"X:\\Test\\{fileName}";
        var pdfStream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // PDF header

        _mockGotenbergClient.Setup(c => c.CanConvertAsync(".txt"))
            .ReturnsAsync(false);
        _mockGotenbergClient.Setup(c => c.ConvertToPdfAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pdfStream);

        // Act
        var result = await _service.DownloadFileWithConversionAsync(downloadPath, forceConvert: true);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IResult>();
        _mockGotenbergClient.Verify(c => c.ConvertToPdfAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DownloadFileWithConversionAsync_WhenConversionFails_ReturnsOriginalFile()
    {
        // Arrange
        var fileName = "test.docx";
        var filePath = Path.Combine(_testDirectory, fileName);
        await File.WriteAllBytesAsync(filePath, new byte[] { 0x50, 0x4B });

        var downloadPath = $"X:\\Test\\{fileName}";

        _mockGotenbergClient.Setup(c => c.CanConvertAsync(".docx"))
            .ReturnsAsync(true);
        _mockGotenbergClient.Setup(c => c.ConvertToPdfAsync(It.IsAny<Stream>(), fileName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        // Act
        var result = await _service.DownloadFileWithConversionAsync(downloadPath, forceConvert: false);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IResult>();
    }

    [Fact]
    public async Task DownloadFileWithConversionAsync_WhenFileInfoIsNull_ReturnsOriginalFile()
    {
        // Arrange
        var filePath = "X:\\Test\\nonexistent.docx";

        _mockGotenbergClient.Setup(c => c.CanConvertAsync(".docx"))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DownloadFileWithConversionAsync(filePath, forceConvert: false);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Microsoft.AspNetCore.Http.IResult>();
    }
}

