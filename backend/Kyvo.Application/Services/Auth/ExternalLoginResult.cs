namespace Kyvo.Application.Services.Auth;

public sealed record ExternalLoginResult
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public required IReadOnlyList<string> PlatformRoles { get; init; }
    public required IReadOnlyList<ExternalLoginTenantMembership> TenantMemberships { get; init; }
}
