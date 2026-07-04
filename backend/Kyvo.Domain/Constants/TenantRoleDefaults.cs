namespace Kyvo.Domain.Constants;

public static class TenantRoleDefaults
{
    public const string OWNER = "owner";
    public const string ADMIN = "admin";
    public const string MEMBER = "member";
    public const string VIEWER = "viewer";

    public static readonly IReadOnlyList<TenantRoleDefinition> All =
    [
        new() { Key = OWNER, Name = "Owner" },
        new() { Key = ADMIN, Name = "Admin" },
        new() { Key = MEMBER, Name = "Member" },
        new() { Key = VIEWER, Name = "Viewer" }
    ];

    public static readonly IReadOnlySet<string> AdministrativeKeys =
        new HashSet<string>([OWNER, ADMIN], StringComparer.OrdinalIgnoreCase);
}
