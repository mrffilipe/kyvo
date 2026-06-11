namespace Kyvo.Application.Services.AppService;

public sealed record ApplicationBrandingDto
{
    public required Guid ApplicationId { get; init; }
    public required bool BrandingEnabled { get; init; }
    public string? BrandingPrimaryColor { get; init; }
    public string? BrandingSecondaryColor { get; init; }
    public string? BrandingLogoUrl { get; init; }
    public string? BrandingHeroTitle { get; init; }
    public string? BrandingHeroSubtitle { get; init; }
}
