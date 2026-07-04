namespace Kyvo.API.Models;

public sealed record CreatedMembershipIdResponse
{
    public required Guid MembershipId { get; init; }
}
