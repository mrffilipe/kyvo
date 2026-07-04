using Kyvo.Application.Queries.Applications.Dtos;
using Kyvo.Application.Queries.Applications.GetApplicationBranding;

namespace Kyvo.Infrastructure.Queries.Applications;

internal static class ApplicationDtoMapper
{
    public static ApplicationClientSummaryDto MapClientSummary(Domain.Entities.ApplicationClient client) =>
        new()
        {
            Id = client.Id,
            ClientId = client.ClientId,
            ClientType = client.ClientType,
            RedirectUris = (IReadOnlyList<string>)client.RedirectUris,
            PostLogoutRedirectUris = (IReadOnlyList<string>)client.PostLogoutRedirectUris,
            AllowedScopes = (IReadOnlyList<string>)client.AllowedScopes,
            AccessTokenTtlSeconds = client.AccessTokenTtlSeconds,
            IsSystem = client.IsSystem
        };

    public static ApplicationBrandingDto MapBrandingDto(Domain.Entities.Application application) =>
        new()
        {
            ApplicationId = application.Id,
            BrandingEnabled = application.BrandingEnabled,
            BrandingPrimaryColor = application.BrandingPrimaryColor,
            BrandingSecondaryColor = application.BrandingSecondaryColor,
            BrandingLogoUrl = application.BrandingLogoPath,
            BrandingHeroTitle = application.BrandingHeroTitle,
            BrandingHeroSubtitle = application.BrandingHeroSubtitle
        };

    public static readonly System.Linq.Expressions.Expression<Func<Domain.Entities.Application, ApplicationDto>> MapToDtoExpression =
        x => new ApplicationDto
        {
            Id = x.Id,
            Name = x.Name,
            Slug = x.Slug,
            Type = x.Type,
            IsSystem = x.IsSystem,
            BrandingEnabled = x.BrandingEnabled,
            BrandingPrimaryColor = x.BrandingPrimaryColor,
            BrandingSecondaryColor = x.BrandingSecondaryColor,
            BrandingLogoUrl = x.BrandingLogoPath,
            BrandingHeroTitle = x.BrandingHeroTitle,
            BrandingHeroSubtitle = x.BrandingHeroSubtitle,
            Clients = null!
        };
}
