using Kyvo.Application.Services.AccountBranding;
using Kyvo.Domain.Repositories;

namespace Kyvo.Infrastructure.Services.AccountBranding;

public sealed class AccountBrandingResolver : IAccountBrandingResolver
{
    private readonly IApplicationClientRepository _clients;

    public AccountBrandingResolver(IApplicationClientRepository clients) => _clients = clients;

    public async Task<AccountBrandingViewModel> ResolveAsync(string? returnUrl, string? clientIdQuery, CancellationToken ct = default)
    {
        var clientId = AccountBrandingClientIdParser.ExtractClientId(returnUrl, clientIdQuery);
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return AccountBrandingViewModel.KyvoDefaults();
        }

        var client = await _clients.GetByClientIdAsync(clientId, ct);
        if (client?.Application is null || !client.Application.HasEffectiveBranding)
        {
            return AccountBrandingViewModel.KyvoDefaults();
        }

        var application = client.Application;
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
