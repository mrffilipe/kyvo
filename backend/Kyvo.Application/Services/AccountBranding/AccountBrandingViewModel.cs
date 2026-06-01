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
            LogoUrl = AccountBrandingDefaults.KyvoLogoUrl,
            PrimaryColor = AccountBrandingDefaults.LightPrimary,
            SecondaryColor = AccountBrandingDefaults.LightSecondary,
            ApplicationName = null,
            HeroTitle = AccountBrandingDefaults.HeroTitle,
            HeroSubtitle = AccountBrandingDefaults.HeroSubtitle
        };
}
