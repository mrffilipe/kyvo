using Kyvo.Domain.Enums;

namespace Kyvo.Application.Services.AppService;

public sealed record ApplicationDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public required ApplicationType Type { get; init; }
    public required bool IsSystem { get; init; }
    public required bool BrandingEnabled { get; init; }
    public string? BrandingPrimaryColor { get; init; }
    public string? BrandingSecondaryColor { get; init; }
    public string? BrandingLogoUrl { get; init; }
    public string? BrandingHeroTitle { get; init; }
    public string? BrandingHeroSubtitle { get; init; }
    public IReadOnlyList<ApplicationClientSummaryDto> Clients { get; init; } = [];
}
