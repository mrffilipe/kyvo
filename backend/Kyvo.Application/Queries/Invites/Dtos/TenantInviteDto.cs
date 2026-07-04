using Kyvo.Domain.Enums;

namespace Kyvo.Application.Queries.Invites.Dtos;

public sealed record TenantInviteDto
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public DateTime? ConsumedAt { get; init; }
    public DateTime? RevokedAt { get; init; }
    public required TenantInviteStatus Status { get; init; }
    public string? AcceptPath { get; init; }
}
