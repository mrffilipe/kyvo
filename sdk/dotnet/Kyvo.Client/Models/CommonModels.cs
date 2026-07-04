namespace Kyvo.Client.Models;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record CreatedIdResponse(Guid Id);

public sealed record CreatedMembershipIdResponse(Guid MembershipId);

public sealed record AvailabilityDto(bool Available);

public enum SessionStatus
{
    Active,
    Revoked,
    Expired,
}
