namespace Kyvo.AspNetCore;

public interface IKyvoUserContext
{
    Guid? UserId { get; }

    Guid? TenantId { get; }

    Guid? MembershipId { get; }

    string? Email { get; }

    IReadOnlyList<string> TenantRoles { get; }

    IReadOnlyList<string> PlatformRoles { get; }

    /// <summary>OAuth client_id from the access token (<c>client_id</c> claim).</summary>
    string? OAuthClientId { get; }

    /// <summary>Token use claim: <c>tenant</c> for tenant-scoped API tokens.</summary>
    string? TokenUse { get; }

    bool IsTenantToken { get; }

    bool HasTenantRole(params string[] roles);

    bool HasPlatformRole(params string[] roles);
}
