using Kyvo.Domain.Enums;

namespace Kyvo.Application.Services.Tenant;

public sealed record TenantInviteDto(
    Guid Id,
    string Email,
    IReadOnlyList<string> Roles,
    DateTime ExpiresAt,
    DateTime? ConsumedAt,
    DateTime? RevokedAt,
    TenantInviteStatus Status,
    string? AcceptPath);
