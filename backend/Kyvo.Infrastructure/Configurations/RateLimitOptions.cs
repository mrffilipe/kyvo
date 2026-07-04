namespace Kyvo.Infrastructure.Configurations;

public sealed record RateLimitOptions
{
    public const string SECTION = "RateLimit";

    public required int AccountRegisterPermitLimit { get; init; }
    public required int AccountRegisterWindowMinutes { get; init; }
}
