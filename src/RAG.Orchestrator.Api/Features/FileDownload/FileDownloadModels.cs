namespace RAG.Orchestrator.Api.Features.FileDownload;

public class SharedFolderConfig
{
    public string Path { get; set; } = string.Empty;
    public string PathToReplace { get; set; } = string.Empty;
}

public class SharedFoldersOptions
{
    public List<SharedFolderConfig> SharedFolders { get; set; } = new();
}

public class FileDownloadError
{
    public string Message { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}