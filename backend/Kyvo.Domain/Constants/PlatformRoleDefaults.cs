namespace Kyvo.Domain.Constants;

public static class PlatformRoleDefaults
{
    public const string CLAIM_TYPE = "prole";
    public const string PLATFORM_ADMINISTRATOR = "plat_admin";

    public static readonly IReadOnlySet<string> AdministrativeKeys =
        new HashSet<string>([PLATFORM_ADMINISTRATOR], StringComparer.OrdinalIgnoreCase);
}
