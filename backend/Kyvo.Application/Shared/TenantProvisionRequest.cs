namespace Kyvo.Application.Shared;

public sealed record TenantProvisionRequest
{
    public required string TenantName { get; init; }
    public required string TenantKey { get; init; }
    public required Guid OwnerUserId { get; init; }
}
