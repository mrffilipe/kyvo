namespace Kyvo.IDP.Application.Configurations;

public sealed class OidcOptions
{
    public const string SECTION = "Oidc";

    public string Issuer { get; set; } = "https://localhost:5101";
    public int RefreshTokenDays { get; set; } = 14;
}

public sealed class GoogleOidcOptions
{
    public const string SECTION = "Google";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public sealed class DevSeedOptions
{
    public const string SECTION = "DevSeed";

    public bool Enabled { get; set; } = true;
    public string AdminEmail { get; set; } = "admin@kyvo.local";
    public string AdminPassword { get; set; } = "ChangeMe!123";
    public string AdminDisplayName { get; set; } = "Kyvo Admin";
}
