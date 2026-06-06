namespace Kyvo.Infrastructure.Configurations;

public sealed class RateLimitOptions
{
    public const string Section = "RateLimit";

    public int AccountRegisterPermitLimit { get; init; } = 5;

    public int AccountRegisterWindowMinutes { get; init; } = 15;
}
