using RAG.Security.Services;

namespace RAG.AddressBook.Services;

/// <summary>
/// Helper service for checking user permissions in AddressBook module
/// </summary>
public interface IAddressBookAuthorizationService
{
    bool CanModifyContacts();
    bool IsAdminOrPowerUser();
    string GetCurrentUserId();
    string? GetCurrentUserName();
}

public class AddressBookAuthorizationService : IAddressBookAuthorizationService
{
    private readonly IUserContextService _userContext;

    public AddressBookAuthorizationService(IUserContextService userContext)
    {
        _userContext = userContext;
    }

    public bool CanModifyContacts()
    {
        return IsAdminOrPowerUser();
    }

    public bool IsAdminOrPowerUser()
    {
        return _userContext.HasAnyRole("Admin", "PowerUser");
    }

    public string GetCurrentUserId()
    {
        return _userContext.GetCurrentUserId() ?? "system";
    }

    public string? GetCurrentUserName()
    {
        return _userContext.GetCurrentUserName();
    }
}
