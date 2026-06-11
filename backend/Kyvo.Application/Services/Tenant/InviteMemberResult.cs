namespace Kyvo.Application.Services.Tenant;

public sealed record InviteMemberResult
{
    public required Guid Id { get; init; }
    public required string AcceptPath { get; init; }
}
