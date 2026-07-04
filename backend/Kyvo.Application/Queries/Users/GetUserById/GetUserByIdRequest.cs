namespace Kyvo.Application.Queries.Users.GetUserById;

public sealed record GetUserByIdRequest
{
    public required Guid UserId { get; init; }
}
