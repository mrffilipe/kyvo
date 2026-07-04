namespace Kyvo.API.Models;

public sealed record ApplicationBrandingLogoResponse
{
    public required string BrandingLogoUrl { get; init; }
}
