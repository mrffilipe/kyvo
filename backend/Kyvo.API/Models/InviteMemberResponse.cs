namespace Kyvo.API.Models;

public sealed record InviteMemberResponse
{
    public required Guid Id { get; init; }
    public required string AcceptPath { get; init; }
}
