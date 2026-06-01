namespace Kyvo.Client.Models;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record CreatedIdResponse(Guid Id);

public sealed record CreatedMembershipIdResponse(Guid MembershipId);

public enum SessionStatus
{
    Active,
    Revoked,
    Expired,
}
