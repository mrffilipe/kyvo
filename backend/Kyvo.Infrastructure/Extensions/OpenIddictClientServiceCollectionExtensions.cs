using Kyvo.Application.Ports.Federation;
using Kyvo.Infrastructure.Services.Federation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenIddict.Client;

namespace Kyvo.Infrastructure.Extensions;

public static class OpenIddictClientServiceCollectionExtensions
{
    /// <summary>
    /// Configures Kyvo as an OAuth 2.0/OIDC *client* of upstream identity providers (Google/Microsoft/GitHub
    /// via OpenIddict.Client.WebIntegration presets, plus any admin-configured generic OIDC issuer). This
    /// replaces the old Firebase Admin SDK + per-provider <c>IIdentityProviderTokenValidator</c> validators:
    /// federation is now handled entirely by OpenIddict's own (externally maintained, security-reviewed)
    /// client stack instead of custom token validation code.
    /// </summary>
    public static IServiceCollection AddKyvoOpenIddictClient(this IServiceCollection services)
    {
        services.AddOpenIddict()
            .AddClient(options =>
            {
                options.AllowAuthorizationCodeFlow();

                // Placeholder URIs satisfy OpenIddict startup validation; OpenIddictClientEndpointConfigurer
                // replaces them with per-provider paths from the database (or keeps placeholders when none exist).
                options.SetRedirectionEndpointUris(OpenIddictClientEndpointConfigurer.RedirectionPlaceholderPath);
                options.SetPostLogoutRedirectionEndpointUris(OpenIddictClientEndpointConfigurer.PostLogoutPlaceholderPath);

                // Certificates only protect the OpenIddict.Client-internal state token; they don't need to
                // be stable across restarts like the server's signing/encryption certificates.
                options.AddDevelopmentEncryptionCertificate();
                options.AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                       .EnableStatusCodePagesIntegration()
                       .EnableRedirectionEndpointPassthrough()
                       .EnablePostLogoutRedirectionEndpointPassthrough();

                options.UseSystemNetHttp();

                // Registers the Google/Microsoft/GitHub presets so DynamicOpenIddictClientService only has
                // to supply ClientId/ClientSecret; discovery, scopes and claim mapping come from the preset.
                options.UseWebProviders();
            });

        // Registered as a singleton (like the default OpenIddictClientService) so the registration cache
        // survives across requests; it creates short-lived DI scopes internally to read the database.
        // Both service types resolve to the same instance so callers (e.g. IdentityProvidersController,
        // to invalidate the cache after an edit) can inject either OpenIddictClientService or the
        // concrete DynamicOpenIddictClientService interchangeably.
        services.AddSingleton<DynamicOpenIddictClientService>();
        services.AddSingleton<OpenIddictClientEndpointConfigurer>();
        services.AddSingleton<IPostConfigureOptions<OpenIddictClientOptions>>(sp =>
            sp.GetRequiredService<OpenIddictClientEndpointConfigurer>());
        services.AddSingleton<IOptionsChangeTokenSource<OpenIddictClientOptions>>(sp =>
            sp.GetRequiredService<OpenIddictClientEndpointConfigurer>());
        services.Replace(ServiceDescriptor.Singleton<OpenIddictClientService>(
            sp => sp.GetRequiredService<DynamicOpenIddictClientService>()));
        services.AddScoped<IFederatedProviderRegistrationCache, FederatedProviderRegistrationCache>();

        return services;
    }
}
