namespace Kyvo.Application.Services.Tenant;

public sealed record ListInvitesByTenantRequest
{
    public Guid TenantId { get; init; }
    public Guid ActorUserId { get; init; }
    public IReadOnlyList<string> ActorPlatformRoles { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
