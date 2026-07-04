namespace Kyvo.Application.Shared;

public sealed record TenantProvisionResult
{
    public required Guid TenantId { get; init; }
    public required Guid MembershipId { get; init; }
    public required Guid OwnerRoleId { get; init; }
    public required string TenantKey { get; init; }
}
