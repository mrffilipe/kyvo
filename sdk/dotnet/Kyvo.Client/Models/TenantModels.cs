namespace Kyvo.Client.Models;

public sealed record TenantDto(
    Guid Id,
    string Name,
    string Key);

public sealed record CreateTenantBody(string Name, string Key);

public sealed record UpdateTenantBody(string Name);

public sealed record InviteMemberBody(string Email, IReadOnlyList<string> Roles);

public sealed record InviteMemberResult(Guid Id, string AcceptPath);

public sealed record TenantInviteDto(
    Guid Id,
    string Email,
    IReadOnlyList<string> Roles,
    DateTime ExpiresAt,
    DateTime? ConsumedAt,
    DateTime? RevokedAt,
    string Status,
    string? AcceptPath);

public sealed record AcceptInviteBody(string Token);
