namespace Kyvo.Application.Configurations;

public sealed record JwtOptions
{
    public const string SECTION = "Jwt";

    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required int RefreshTokenDays { get; init; }
    public string? SigningKeyPath { get; init; }
    public string? SigningKeyPem { get; init; }
    public string? SigningKeyPemBase64 { get; init; }
    public required string KeyId { get; init; }
}
