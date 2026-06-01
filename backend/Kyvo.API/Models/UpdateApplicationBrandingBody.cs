namespace Kyvo.API.Models;

public sealed record UpdateApplicationBrandingBody
{
    public required bool BrandingEnabled { get; init; }

    public string? BrandingPrimaryColor { get; init; }

    public string? BrandingSecondaryColor { get; init; }

    public string? BrandingHeroTitle { get; init; }

    public string? BrandingHeroSubtitle { get; init; }
}
