using Kyvo.Domain.Common;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Exceptions;

namespace Kyvo.Domain.Entities;

public class Application : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public ApplicationType Type { get; private set; }

    /// <summary>
    /// Indicates that this application is managed by the platform and cannot be edited or removed via API.
    /// Example: the admin console created automatically during bootstrap.
    /// </summary>
    public bool IsSystem { get; private set; }

    public bool BrandingEnabled { get; private set; }

    public string? BrandingPrimaryColor { get; private set; }

    public string? BrandingSecondaryColor { get; private set; }

    /// <summary>
    /// Web-relative path to the uploaded logo (e.g. /branding/{applicationId}/logo.png).
    /// </summary>
    public string? BrandingLogoPath { get; private set; }

    public string? BrandingHeroTitle { get; private set; }

    public string? BrandingHeroSubtitle { get; private set; }

    public const int BrandingHeroTitleMaxLength = 200;

    public const int BrandingHeroSubtitleMaxLength = 500;

    public ICollection<ApplicationClient> Clients { get; private set; } = new List<ApplicationClient>();
    public ICollection<ApplicationTenant> Tenants { get; private set; } = new List<ApplicationTenant>();

    public bool HasEffectiveBranding =>
        BrandingEnabled &&
        !IsSystem &&
        !string.IsNullOrWhiteSpace(BrandingPrimaryColor) &&
        !string.IsNullOrWhiteSpace(BrandingSecondaryColor);

    private Application()
    {
    }

    public Application(
        string name,
        string slug,
        ApplicationType type,
        bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(slug))
        {
            throw new DomainValidationException(DomainErrorMessages.Application.NameAndSlugRequired);
        }

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Type = type;
        IsSystem = isSystem;
    }

    public void UpdateBranding(
        bool enabled,
        string? primaryColor,
        string? secondaryColor,
        string? heroTitle,
        string? heroSubtitle)
    {
        if (IsSystem)
        {
            throw new DomainValidationException(DomainErrorMessages.Application.BrandingSystemApplicationReadOnly);
        }

        if (!enabled)
        {
            BrandingEnabled = false;
            return;
        }

        BrandingPrimaryColor = BrandingColor.NormalizeOrThrow(primaryColor, "Primary color");
        BrandingSecondaryColor = BrandingColor.NormalizeOrThrow(secondaryColor, "Secondary color");
        BrandingHeroTitle = NormalizeOptionalHeroText(
            heroTitle,
            BrandingHeroTitleMaxLength,
            DomainErrorMessages.Application.BrandingHeroTitleTooLong);
        BrandingHeroSubtitle = NormalizeOptionalHeroText(
            heroSubtitle,
            BrandingHeroSubtitleMaxLength,
            DomainErrorMessages.Application.BrandingHeroSubtitleTooLong);
        BrandingEnabled = true;
    }

    private static string? NormalizeOptionalHeroText(string? value, int maxLength, string tooLongMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainValidationException(string.Format(tooLongMessage, maxLength));
        }

        return trimmed;
    }

    public void SetBrandingLogoPath(string relativePath)
    {
        if (IsSystem)
        {
            throw new DomainValidationException(DomainErrorMessages.Application.BrandingSystemApplicationReadOnly);
        }

        if (string.IsNullOrWhiteSpace(relativePath) ||
            !relativePath.StartsWith("/branding/", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException(DomainErrorMessages.Application.BrandingLogoPathInvalid);
        }

        BrandingLogoPath = relativePath.Trim();
    }

    public void ClearBrandingLogoPath()
    {
        BrandingLogoPath = null;
    }
}
