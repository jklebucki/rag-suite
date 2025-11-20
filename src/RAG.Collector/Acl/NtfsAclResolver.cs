using System.DirectoryServices.AccountManagement;
using System.Security.AccessControl;
using System.Security.Principal;

namespace RAG.Collector.Acl;

/// <summary>
/// NTFS ACL resolver that maps Windows file permissions to Active Directory group names
/// </summary>
public class NtfsAclResolver : IAclResolver
{
    private readonly ILogger<NtfsAclResolver> _logger;

    public NtfsAclResolver(ILogger<NtfsAclResolver> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns true on Windows platforms where NTFS ACL resolution is supported
    /// </summary>
    public bool IsSupported => OperatingSystem.IsWindows();

    /// <summary>
    /// Resolves NTFS ACL for the specified path to Active Directory group names
    /// </summary>
    /// <param name="filePath">File or folder path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of AD group names with access to the resource</returns>
    public async Task<List<string>> ResolveAclGroupsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!IsSupported)
        {
            _logger.LogWarning("ACL resolution is not supported on this platform");
            return new List<string>();
        }

        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("ACL resolution requires Windows platform");
            return new List<string>();
        }

        try
        {
            _logger.LogDebug("Resolving ACL for path: {FilePath}", filePath);

            var aclGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Get file/directory security info
            AuthorizationRuleCollection accessRules;

            if (File.Exists(filePath))
            {
                var fileSecurity = new FileSecurity(filePath, AccessControlSections.Access);
                accessRules = fileSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
            }
            else
            {
                var directorySecurity = new DirectorySecurity(filePath, AccessControlSections.Access);
                accessRules = directorySecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
            }

            foreach (AuthorizationRule rule in accessRules)
            {
                if (rule is FileSystemAccessRule accessRule &&
                    accessRule.AccessControlType == AccessControlType.Allow &&
                    (accessRule.FileSystemRights & FileSystemRights.Read) == FileSystemRights.Read)
                {
                    var sid = (SecurityIdentifier)accessRule.IdentityReference;
                    var groupName = await ResolveSidToGroupNameAsync(sid, cancellationToken);

                    if (!string.IsNullOrEmpty(groupName))
                    {
                        aclGroups.Add(groupName);
                    }
                }
            }

            var result = aclGroups.ToList();
            _logger.LogDebug("Resolved {Count} ACL groups for {FilePath}: {Groups}",
                result.Count, filePath, string.Join(", ", result));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve ACL for path: {FilePath}", filePath);
            return new List<string>();
        }
    }

    /// <summary>
    /// Resolves Security Identifier (SID) to Active Directory group name
    /// </summary>
    /// <param name="sid">Security Identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Group name (UPN or CN) or null if resolution fails</returns>
    private async Task<string?> ResolveSidToGroupNameAsync(SecurityIdentifier sid, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            // First try to translate SID to NTAccount
            var account = (NTAccount)sid.Translate(typeof(NTAccount));
            var accountName = account.Value;

            _logger.LogTrace("Translated SID {Sid} to account: {Account}", sid, accountName);

            // Skip well-known system accounts
            if (IsSystemAccount(accountName))
            {
                _logger.LogTrace("Skipping system account: {Account}", accountName);
                return null;
            }

            // Try to resolve through Active Directory
            return await Task.Run(() => ResolveAccountNameThroughAD(accountName), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("SID resolution failed for {Sid}: {ExceptionType} - {Message}", sid, ex.GetType().Name, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Resolves account name through Active Directory to get proper group name
    /// </summary>
    /// <param name="accountName">NT account name (DOMAIN\Account)</param>
    /// <returns>Resolved group name or original account name</returns>
    private string? ResolveAccountNameThroughAD(string accountName)
    {
        if (!OperatingSystem.IsWindows())
        {
            return accountName;
        }

        try
        {
            // Parse domain and account name
            var parts = accountName.Split('\\');
            if (parts.Length != 2)
            {
                return accountName; // Return as-is if not in DOMAIN\Account format
            }

            var domain = parts[0];
            var name = parts[1];

            // Skip resolution for built-in domains and well-known authorities
            if (string.Equals(domain, "BUILTIN", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(domain, "NT AUTHORITY", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(domain, "LOCAL AUTHORITY", StringComparison.OrdinalIgnoreCase))
            {
                return accountName;
            }

            // Determine context type based on domain part
            var contextType = string.Equals(domain, Environment.MachineName, StringComparison.OrdinalIgnoreCase)
                ? ContextType.Machine
                : ContextType.Domain;

            try
            {
                // Try to find the principal in Active Directory or Local Machine
                using var context = new PrincipalContext(contextType, domain);
                using var principal = Principal.FindByIdentity(context, IdentityType.SamAccountName, name);

                if (principal != null)
                {
                    // Prefer UserPrincipalName, fall back to Name, then SamAccountName
                    return principal.UserPrincipalName ?? principal.Name ?? principal.SamAccountName;
                }
            }
            catch (Exception ex) when (ex is PrincipalException or ArgumentException or System.Runtime.InteropServices.COMException)
            {
                // Common exceptions when AD is not reachable or domain is invalid
                _logger.LogDebug("AD principal resolution failed for {Name}@{Domain}: {ExceptionType} - {Message}", 
                    name, domain, ex.GetType().Name, ex.Message);
                return accountName;
            }

            return accountName; // Return original if not found in AD
        }
        catch (Exception ex)
        {
            _logger.LogDebug("AD resolution error for {AccountName}: {ExceptionType} - {Message}", 
                accountName, ex.GetType().Name, ex.Message);
            return accountName; // Return original on error
        }
    }

    /// <summary>
    /// Checks if the account is a well-known system account that should be excluded
    /// </summary>
    /// <param name="accountName">Account name to check</param>
    /// <returns>True if this is a system account to exclude</returns>
    private static bool IsSystemAccount(string accountName)
    {
        var systemAccounts = new[]
        {
            "NT AUTHORITY\\SYSTEM",
            "BUILTIN\\Administrators",
            "BUILTIN\\Users",
            "CREATOR OWNER",
            "NT AUTHORITY\\Authenticated Users",
            "BUILTIN\\CREATOR OWNER"
        };

        return systemAccounts.Any(sysAccount =>
            string.Equals(accountName, sysAccount, StringComparison.OrdinalIgnoreCase));
    }
}
