namespace Kyvo.Client.Models;

public sealed record TenantDto(
    Guid Id,
    string Name,
    string Key);

public sealed record CreateTenantBody(string Name, string Key);

public sealed record UpdateTenantBody(string Name);

public sealed record InviteMemberBody(string Email, IReadOnlyList<string> Roles);

public sealed record AcceptInviteBody(string Token);
