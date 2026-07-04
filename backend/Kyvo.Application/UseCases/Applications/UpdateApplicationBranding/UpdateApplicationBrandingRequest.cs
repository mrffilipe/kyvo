namespace Kyvo.Application.UseCases.Applications.UpdateApplicationBranding;

public sealed record UpdateApplicationBrandingRequest
{
    public required Guid ApplicationId { get; init; }
    public required bool BrandingEnabled { get; init; }
    public string? BrandingPrimaryColor { get; init; }
    public string? BrandingSecondaryColor { get; init; }
    public string? BrandingHeroTitle { get; init; }
    public string? BrandingHeroSubtitle { get; init; }
    public required IReadOnlyList<string> ActorPlatformRoles { get; init; }
}
