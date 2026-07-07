using Kyvo.Domain.Common;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Exceptions;

namespace Kyvo.Domain.Entities;

public sealed class Application : BaseEntity
{
    public const int BRANDING_HERO_TITLE_MAX_LENGTH = 200;
    public const int BRANDING_HERO_SUBTITLE_MAX_LENGTH = 500;

    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public ApplicationType Type { get; private set; }
    public bool BrandingEnabled { get; private set; }
    public string? BrandingPrimaryColor { get; private set; }
    public string? BrandingSecondaryColor { get; private set; }
    public string? BrandingHeroTitle { get; private set; }
    public string? BrandingHeroSubtitle { get; private set; }
    public bool HasEffectiveBranding =>
            BrandingEnabled &&
            !IsSystem &&
            !string.IsNullOrWhiteSpace(BrandingPrimaryColor) &&
            !string.IsNullOrWhiteSpace(BrandingSecondaryColor);

    /// <summary>
    /// Indicates that this application is managed by the platform and cannot be edited or removed via API.
    /// Example: the admin console created automatically during bootstrap.
    /// </summary>
    public bool IsSystem { get; private set; }

    /// <summary>
    /// Web-relative path to the uploaded logo (e.g. /branding/{applicationId}/logo.png).
    /// </summary>
    public string? BrandingLogoPath { get; private set; }

    public ICollection<ApplicationTenant> Tenants { get; private set; } = [];

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
            throw new DomainValidationException(DomainErrorMessages.Application.NAME_AND_SLUG_REQUIRED);
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
            throw new DomainValidationException(DomainErrorMessages.Application.BRANDING_SYSTEM_APPLICATION_READ_ONLY);
        }

        if (!enabled)
        {
            BrandingEnabled = false;
            return;
        }

        BrandingPrimaryColor = BrandingColor.NormalizeOrThrow(primaryColor, DomainErrorMessages.Application.BRANDING_PRIMARY_COLOR_FIELD);
        BrandingSecondaryColor = BrandingColor.NormalizeOrThrow(secondaryColor, DomainErrorMessages.Application.BRANDING_SECONDARY_COLOR_FIELD);
        BrandingHeroTitle = NormalizeOptionalHeroText(
            heroTitle,
            BRANDING_HERO_TITLE_MAX_LENGTH,
            DomainErrorMessages.Application.BRANDING_HERO_TITLE_TOO_LONG);
        BrandingHeroSubtitle = NormalizeOptionalHeroText(
            heroSubtitle,
            BRANDING_HERO_SUBTITLE_MAX_LENGTH,
            DomainErrorMessages.Application.BRANDING_HERO_SUBTITLE_TOO_LONG);
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
            throw new DomainValidationException(DomainErrorMessages.Application.BRANDING_SYSTEM_APPLICATION_READ_ONLY);
        }

        if (string.IsNullOrWhiteSpace(relativePath) ||
            !relativePath.StartsWith("/branding/", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException(DomainErrorMessages.Application.BRANDING_LOGO_PATH_INVALID);
        }

        BrandingLogoPath = relativePath.Trim();
    }

    public void ClearBrandingLogoPath()
    {
        BrandingLogoPath = null;
    }
}
