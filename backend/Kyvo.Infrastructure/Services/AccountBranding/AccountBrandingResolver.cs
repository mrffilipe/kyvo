using Kyvo.Application.Ports.Oidc;
using Kyvo.Application.Services.AccountBranding;
using Kyvo.Domain.Repositories;

namespace Kyvo.Infrastructure.Services.AccountBranding;

public sealed class AccountBrandingResolver : IAccountBrandingResolver
{
    private readonly IOAuthClientManager _oauthClients;
    private readonly IApplicationRepository _applications;

    public AccountBrandingResolver(IOAuthClientManager oauthClients, IApplicationRepository applications)
    {
        _oauthClients = oauthClients;
        _applications = applications;
    }

    public async Task<AccountBrandingViewModel> ResolveAsync(string? returnUrl, string? clientIdQuery, CancellationToken ct = default)
    {
        var clientId = AccountBrandingClientIdParser.ExtractClientId(returnUrl, clientIdQuery);
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return AccountBrandingViewModel.KyvoDefaults();
        }

        var client = await _oauthClients.GetByClientIdAsync(clientId, ct);
        if (client is null)
        {
            return AccountBrandingViewModel.KyvoDefaults();
        }

        var application = await _applications.GetByIdAsync(client.ApplicationId, ct);
        if (application is null || !application.HasEffectiveBranding)
        {
            return AccountBrandingViewModel.KyvoDefaults();
        }

        var logoUrl = !string.IsNullOrWhiteSpace(application.BrandingLogoPath)
            ? application.BrandingLogoPath
            : AccountBrandingDefaults.KYVO_LOGO_URL;

        return new AccountBrandingViewModel
        {
            UseKyvoDefaults = false,
            LogoUrl = logoUrl,
            PrimaryColor = application.BrandingPrimaryColor!,
            SecondaryColor = application.BrandingSecondaryColor!,
            ApplicationName = application.Name,
            HeroTitle = string.IsNullOrWhiteSpace(application.BrandingHeroTitle)
                ? AccountBrandingDefaults.HERO_TITLE
                : application.BrandingHeroTitle,
            HeroSubtitle = string.IsNullOrWhiteSpace(application.BrandingHeroSubtitle)
                ? AccountBrandingDefaults.HERO_SUBTITLE
                : application.BrandingHeroSubtitle
        };
    }
}
