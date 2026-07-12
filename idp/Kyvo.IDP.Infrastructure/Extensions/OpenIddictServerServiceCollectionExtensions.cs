using Kyvo.IDP.Application.Configurations;
using Kyvo.IDP.Infrastructure.Persistence;
using Kyvo.IDP.Infrastructure.Services.Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenIddict.Abstractions;

namespace Kyvo.IDP.Infrastructure.Extensions;

public static class OpenIddictServerServiceCollectionExtensions
{
    public static IServiceCollection AddKyvoOpenIddictServer(this IServiceCollection services, IConfiguration configuration)
    {
        var oidcOptions = configuration.GetSection(OidcOptions.SECTION).Get<OidcOptions>()
            ?? new OidcOptions();

        var signingProvider = new SigningCertificateProvider(configuration);
        services.TryAddSingleton(signingProvider);
        var certificate = signingProvider.Certificate;

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>();
            })
            .AddServer(options =>
            {
                options.SetIssuer(new Uri(oidcOptions.Issuer));

                options.SetAuthorizationEndpointUris("connect/authorize")
                       .SetTokenEndpointUris("connect/token")
                       .SetUserInfoEndpointUris("connect/userinfo")
                       .SetEndSessionEndpointUris("connect/logout")
                       .SetRevocationEndpointUris("connect/revoke")
                       .SetIntrospectionEndpointUris("connect/introspect");

                options.AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange();
                options.AllowRefreshTokenFlow();

                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.OfflineAccess);

                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(oidcOptions.RefreshTokenDays));
                options.SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(10));

                options.AddSigningCertificate(certificate)
                       .AddEncryptionCertificate(certificate);

                var aspNetCore = options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough()
                       .EnableEndSessionEndpointPassthrough()
                       .EnableStatusCodePagesIntegration();

                // Allow HTTP in Development / Docker Compose; require HTTPS in production hosts.
                if (string.Equals(
                        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                        "Development",
                        StringComparison.OrdinalIgnoreCase))
                {
                    aspNetCore.DisableTransportSecurityRequirement();
                }
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }
}
