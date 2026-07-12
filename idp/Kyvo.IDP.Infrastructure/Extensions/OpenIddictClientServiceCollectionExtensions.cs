using Kyvo.IDP.Application.Configurations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kyvo.IDP.Infrastructure.Extensions;

public static class OpenIddictClientServiceCollectionExtensions
{
    public const string GoogleRegistrationId = "google";

    public static IServiceCollection AddKyvoOpenIddictClient(this IServiceCollection services, IConfiguration configuration)
    {
        var google = configuration.GetSection(GoogleOidcOptions.SECTION).Get<GoogleOidcOptions>()
            ?? new GoogleOidcOptions();

        var googleConfigured = !string.IsNullOrWhiteSpace(google.ClientId)
            && !string.IsNullOrWhiteSpace(google.ClientSecret);

        services.AddOpenIddict()
            .AddClient(options =>
            {
                options.AllowAuthorizationCodeFlow();

                // Required whenever authorization code flow is enabled (even before Google secrets exist).
                options.SetRedirectionEndpointUris("callback/login/google");
                options.SetPostLogoutRedirectionEndpointUris("callback/logout/google");

                options.AddDevelopmentEncryptionCertificate();
                options.AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                       .EnableStatusCodePagesIntegration()
                       .EnableRedirectionEndpointPassthrough()
                       .EnablePostLogoutRedirectionEndpointPassthrough();

                options.UseSystemNetHttp();

                if (googleConfigured)
                {
                    options.UseWebProviders()
                           .AddGoogle(googleOptions =>
                           {
                               googleOptions.SetClientId(google.ClientId)
                                            .SetClientSecret(google.ClientSecret)
                                            .SetRedirectUri("callback/login/google")
                                            .SetProviderName(GoogleRegistrationId);
                           });
                }
            });

        return services;
    }
}
