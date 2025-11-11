using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RAG.Collector.Acl;
using RAG.Collector.Enumerators;
using RAG.Collector.Models;

namespace RAG.Tests.Collector;

public class FileEnumeratorTests : IDisposable
{
    private readonly Mock<ILogger<FileEnumerator>> _mockLogger;
    private readonly Mock<IAclResolver> _mockAclResolver;
    private readonly FileEnumerator _enumerator;
    private readonly string _testDirectory;

    public FileEnumeratorTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _mockLogger = new Mock<ILogger<FileEnumerator>>();
        _mockAclResolver = new Mock<IAclResolver>();

        _mockAclResolver
            .Setup(x => x.ResolveAclGroupsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _enumerator = new FileEnumerator(_mockLogger.Object, _mockAclResolver.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task EnumerateFilesAsync_WithNonExistentFolder_ReturnsEmpty()
    {
        // Arrange
        var folders = new[] { Path.Combine(_testDirectory, "nonexistent") };
        var extensions = new[] { ".txt" };

        // Act
        var files = new List<FileItem>();
        await foreach (var file in _enumerator.EnumerateFilesAsync(folders, extensions))
        {
            files.Add(file);
        }

        // Assert
        files.Should().BeEmpty();
    }

    [Fact]
    public async Task EnumerateFilesAsync_WithMatchingFiles_ReturnsFiles()
    {
        // Arrange
        var testFile1 = Path.Combine(_testDirectory, "test1.txt");
        var testFile2 = Path.Combine(_testDirectory, "test2.txt");
        await File.WriteAllTextAsync(testFile1, "content1");
        await File.WriteAllTextAsync(testFile2, "content2");

        var folders = new[] { _testDirectory };
        var extensions = new[] { ".txt" };

        // Act
        var files = new List<FileItem>();
        await foreach (var file in _enumerator.EnumerateFilesAsync(folders, extensions))
        {
            files.Add(file);
        }

        // Assert
        files.Should().HaveCount(2);
        files.Select(f => f.Path).Should().Contain(new[] { testFile1, testFile2 });
    }

    [Fact]
    public async Task EnumerateFilesAsync_WithNonMatchingExtensions_ReturnsEmpty()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test.txt");
        await File.WriteAllTextAsync(testFile, "content");

        var folders = new[] { _testDirectory };
        var extensions = new[] { ".pdf" };

        // Act
        var files = new List<FileItem>();
        await foreach (var file in _enumerator.EnumerateFilesAsync(folders, extensions))
        {
            files.Add(file);
        }

        // Assert
        files.Should().BeEmpty();
    }

    [Fact]
    public async Task EnumerateFilesAsync_WithSubdirectories_ReturnsFilesFromSubdirectories()
    {
        // Arrange
        var subDir = Path.Combine(_testDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        var testFile = Path.Combine(subDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "content");

        var folders = new[] { _testDirectory };
        var extensions = new[] { ".txt" };

        // Act
        var files = new List<FileItem>();
        await foreach (var file in _enumerator.EnumerateFilesAsync(folders, extensions))
        {
            files.Add(file);
        }

        // Assert
        files.Should().HaveCount(1);
        files[0].Path.Should().Be(testFile);
    }

    [Fact]
    public async Task EnumerateFilesAsync_WithMultipleFolders_ReturnsFilesFromAllFolders()
    {
        // Arrange
        var folder1 = Path.Combine(_testDirectory, "folder1");
        var folder2 = Path.Combine(_testDirectory, "folder2");
        Directory.CreateDirectory(folder1);
        Directory.CreateDirectory(folder2);

        var file1 = Path.Combine(folder1, "test1.txt");
        var file2 = Path.Combine(folder2, "test2.txt");
        await File.WriteAllTextAsync(file1, "content1");
        await File.WriteAllTextAsync(file2, "content2");

        var folders = new[] { folder1, folder2 };
        var extensions = new[] { ".txt" };

        // Act
        var files = new List<FileItem>();
        await foreach (var file in _enumerator.EnumerateFilesAsync(folders, extensions))
        {
            files.Add(file);
        }

        // Assert
        files.Should().HaveCount(2);
    }

    [Fact]
    public async Task EnumerateFilesAsync_WithEmptyExtensions_ReturnsAllFiles()
    {
        // Arrange
        var testFile1 = Path.Combine(_testDirectory, "test1.txt");
        var testFile2 = Path.Combine(_testDirectory, "test2.md");
        await File.WriteAllTextAsync(testFile1, "content1");
        await File.WriteAllTextAsync(testFile2, "content2");

        var folders = new[] { _testDirectory };
        var extensions = Array.Empty<string>();

        // Act
        var files = new List<FileItem>();
        await foreach (var file in _enumerator.EnumerateFilesAsync(folders, extensions))
        {
            files.Add(file);
        }

        // Assert
        files.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFileCountAsync_WithMatchingFiles_ReturnsCorrectCount()
    {
        // Arrange
        var testFile1 = Path.Combine(_testDirectory, "test1.txt");
        var testFile2 = Path.Combine(_testDirectory, "test2.txt");
        await File.WriteAllTextAsync(testFile1, "content1");
        await File.WriteAllTextAsync(testFile2, "content2");

        var folders = new[] { _testDirectory };
        var extensions = new[] { ".txt" };

        // Act
        var count = await _enumerator.GetFileCountAsync(folders, extensions);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetFileCountAsync_WithNonMatchingExtensions_ReturnsZero()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test.txt");
        await File.WriteAllTextAsync(testFile, "content");

        var folders = new[] { _testDirectory };
        var extensions = new[] { ".pdf" };

        // Act
        var count = await _enumerator.GetFileCountAsync(folders, extensions);

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task EnumerateFilesAsync_WithCancellation_StopsEnumeration()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test.txt");
        await File.WriteAllTextAsync(testFile, "content");

        var folders = new[] { _testDirectory };
        var extensions = new[] { ".txt" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var files = new List<FileItem>();
        await foreach (var file in _enumerator.EnumerateFilesAsync(folders, extensions, cts.Token))
        {
            files.Add(file);
        }

        // Assert
        files.Should().BeEmpty();
    }

    [Fact]
    public async Task EnumerateFilesAsync_WithFileItem_ContainsCorrectProperties()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test.txt");
        var content = "test content";
        await File.WriteAllTextAsync(testFile, content);
        var fileInfo = new FileInfo(testFile);

        var folders = new[] { _testDirectory };
        var extensions = new[] { ".txt" };

        // Act
        var files = new List<FileItem>();
        await foreach (var file in _enumerator.EnumerateFilesAsync(folders, extensions))
        {
            files.Add(file);
        }

        // Assert
        files.Should().HaveCount(1);
        files[0].Path.Should().Be(testFile);
        files[0].Extension.Should().Be(".txt");
        files[0].Size.Should().Be(content.Length);
        files[0].LastWriteTimeUtc.Should().BeCloseTo(fileInfo.LastWriteTimeUtc, TimeSpan.FromSeconds(1));
    }
}

