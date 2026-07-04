namespace Kyvo.Application.Queries.Platform.GetPlatformStatus;

public sealed record PlatformStatusResult
{
    public required bool IsConfigured { get; init; }
    public required bool RequiresBootstrap { get; init; }
    public string? OauthClientId { get; init; }
}
