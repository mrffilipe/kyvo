namespace Kyvo.Application.UseCases.Invites.InviteMember;

public sealed record InviteMemberResult
{
    public required Guid Id { get; init; }
    public required string AcceptPath { get; init; }
}
