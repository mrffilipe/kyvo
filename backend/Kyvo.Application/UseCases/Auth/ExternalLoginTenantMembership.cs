namespace Kyvo.Application.UseCases.Auth;

public sealed record ExternalLoginTenantMembership
{
    public required Guid TenantId { get; init; }
    public required Guid MembershipId { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
}
