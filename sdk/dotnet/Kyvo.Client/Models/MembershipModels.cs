namespace Kyvo.Client.Models;

public sealed record MembershipDto(
    Guid Id,
    Guid UserId,
    string? UserEmail,
    string? UserDisplayName,
    Guid TenantId,
    IReadOnlyList<string> Roles,
    bool IsActive);

public sealed record CreateMembershipBody(Guid UserId, IReadOnlyList<string> Roles);

public sealed record UpdateMembershipRolesBody(IReadOnlyList<string> Roles);
