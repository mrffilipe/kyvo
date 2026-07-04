namespace Kyvo.Client.Models;

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? PhotoUrl,
    IReadOnlyList<UserMembershipDto> Memberships);

public sealed record UpdateUserProfileBody(string DisplayName, string? PhotoUrl = null);

public sealed record UserMembershipDto(
    Guid MembershipId,
    Guid TenantId,
    string TenantName,
    string TenantKey,
    IReadOnlyList<string> Roles);
