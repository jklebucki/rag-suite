namespace RAG.Security.Models;

public static class UserRoles
{
    public const string User = "User";
    public const string PowerUser = "PowerUser";
    public const string Admin = "Admin";
    
    public static readonly string[] AllRoles = { User, PowerUser, Admin };
    
    public static readonly Dictionary<string, string> RoleDescriptions = new()
    {
        { User, "Basic user with standard access to chat functionality" },
        { PowerUser, "Advanced user with extended features and capabilities" },
        { Admin, "Administrator with full system access and user management" }
    };
}
