namespace RAG.Collector.Acl;

/// <summary>
/// Interface for resolving file/folder Access Control Lists (ACL) to security groups
/// </summary>
public interface IAclResolver
{
    /// <summary>
    /// Resolves file/folder ACL to a list of security group names
    /// </summary>
    /// <param name="filePath">Path to the file or folder</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of group names that have access to the resource</returns>
    Task<List<string>> ResolveAclGroupsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether ACL resolution is supported on the current platform
    /// </summary>
    bool IsSupported { get; }
}
