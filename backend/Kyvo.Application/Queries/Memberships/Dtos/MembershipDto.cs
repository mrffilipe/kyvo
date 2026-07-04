namespace Kyvo.Application.Queries.Memberships.Dtos;

public sealed record MembershipDto
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public string? UserEmail { get; init; }
    public string? UserDisplayName { get; init; }
    public required Guid TenantId { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
    public required bool IsActive { get; init; }
}
