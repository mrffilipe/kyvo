namespace Kyvo.Application.Services.AccountBranding;

public sealed record AccountBrandingViewModel
{
    public required bool UseKyvoDefaults { get; init; }
    public required string LogoUrl { get; init; }
    public required string PrimaryColor { get; init; }
    public required string SecondaryColor { get; init; }
    public string? ApplicationName { get; init; }
    public required string HeroTitle { get; init; }
    public required string HeroSubtitle { get; init; }

    public static AccountBrandingViewModel KyvoDefaults() =>
        new()
        {
            UseKyvoDefaults = true,
            LogoUrl = AccountBrandingDefaults.KYVO_LOGO_URL,
            PrimaryColor = AccountBrandingDefaults.LIGHT_PRIMARY,
            SecondaryColor = AccountBrandingDefaults.LIGHT_SECONDARY,
            ApplicationName = null,
            HeroTitle = AccountBrandingDefaults.HERO_TITLE,
            HeroSubtitle = AccountBrandingDefaults.HERO_SUBTITLE
        };
}
