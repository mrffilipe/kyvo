namespace Kyvo.Infrastructure.Configurations;

public sealed record InviteOptions
{
    public const string SECTION = "Invite";

    public required int ExpirationHours { get; init; }
}
