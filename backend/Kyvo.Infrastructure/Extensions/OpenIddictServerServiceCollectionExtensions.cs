using Kyvo.Application.Configurations;
using Kyvo.Infrastructure.Oidc;
using Kyvo.Infrastructure.Persistence;
using Kyvo.Infrastructure.Persistence.Entities;
using Kyvo.Infrastructure.Services.Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;

namespace Kyvo.Infrastructure.Extensions;

public static class OpenIddictServerServiceCollectionExtensions
{
    /// <summary>
    /// Configures the OpenIddict authorization server (authorization code + PKCE, refresh tokens, userinfo,
    /// RP-initiated logout) and the OpenIddict validation stack used to protect the versioned JSON API.
    /// </summary>
    public static IServiceCollection AddKyvoOpenIddictServer(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SECTION).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");

        var signingProvider = new SigningCertificateProvider(configuration);
        services.TryAddSingleton(signingProvider);
        var certificate = signingProvider.Certificate;

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>()
                       .ReplaceDefaultEntities<
                           KyvoOpenIddictApplication,
                           KyvoOpenIddictAuthorization,
                           OpenIddictEntityFrameworkCoreScope<Guid>,
                           KyvoOpenIddictToken,
                           Guid>();
            })
            .AddServer(options =>
            {
                options.SetIssuer(new Uri(jwtOptions.Issuer));

                options.SetAuthorizationEndpointUris("connect/authorize")
                       .SetTokenEndpointUris("connect/token")
                       .SetUserInfoEndpointUris("connect/userinfo")
                       .SetEndSessionEndpointUris("connect/logout")
                       .SetRevocationEndpointUris("connect/revoke")
                       .SetIntrospectionEndpointUris("connect/introspect");

                options.AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange();
                options.AllowRefreshTokenFlow();
                // Rolling refresh tokens are enabled by default in OpenIddict 7 (each redemption issues a new refresh token).

                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.OfflineAccess);

                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(jwtOptions.RefreshTokenDays));
                options.SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(10));

                options.AddSigningCertificate(certificate)
                       .AddEncryptionCertificate(certificate);

                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough()
                       .EnableEndSessionEndpointPassthrough()
                       .EnableStatusCodePagesIntegration()
                       .DisableTransportSecurityRequirement();

                options.AddEventHandler(ValidateAuthSessionHandler.Descriptor)
                       .AddEventHandler(ValidateAdminConsoleAccessHandler.Descriptor)
                       .AddEventHandler(ApplyClientAccessTokenLifetimeHandler.Descriptor)
                       .AddEventHandler(TouchAuthSessionHandler.Descriptor);
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
                options.AddAudiences(jwtOptions.Audience);
            });

        return services;
    }
}
