using System.Collections.Concurrent;
using System.Text.Json;
using Kyvo.Application.IdentityProviderConfigs;
using Kyvo.Application.Services.IdentityProvider;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Client;
using OpenIddict.Client.WebIntegration;

namespace Kyvo.Infrastructure.Services.Federation;

/// <summary>
/// Resolves <see cref="OpenIddictClientRegistration"/> instances at runtime from the admin-configured
/// <c>IdentityProvider</c> rows instead of hard-coded startup registrations. This is the officially
/// documented pattern for "dynamic" OpenIddict.Client registrations (OpenIddict does not support dynamic
/// client registration out of the box): override <see cref="GetClientRegistrationByIdAsync"/> and
/// <see cref="GetClientRegistrationByIssuerAsync"/> and cache the results.
///
/// Google/Microsoft/GitHub reuse the battle-tested OpenIddict.Client.WebIntegration presets; any other
/// OIDC-compliant provider (Cognito, Auth0, Keycloak, a partner's own IdP, ...) is configured generically
/// from its discovery document (<see cref="IdentityProviderType.GenericOidc"/>).
/// </summary>
public sealed class DynamicOpenIddictClientService : OpenIddictClientService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IServiceProvider _provider;
    private readonly ConcurrentDictionary<string, OpenIddictClientRegistration> _registrationsByAlias =
        new(StringComparer.OrdinalIgnoreCase);

    public DynamicOpenIddictClientService(IServiceProvider provider) : base(provider)
    {
        _provider = provider;
    }

    /// <summary>Called by the FederatedLoginController before every challenge so a config edit takes effect immediately.</summary>
    public void Invalidate(string alias) => _registrationsByAlias.TryRemove(alias, out _);

    public override async ValueTask<OpenIddictClientRegistration> GetClientRegistrationByIdAsync(
        string identifier, CancellationToken cancellationToken = default)
    {
        if (_registrationsByAlias.TryGetValue(identifier, out var cached))
        {
            return cached;
        }

        using var scope = _provider.CreateScope();
        var identityProviders = scope.ServiceProvider.GetRequiredService<IIdentityProviderRepository>();
        var configCipher = scope.ServiceProvider.GetRequiredService<IIdentityProviderConfigCipher>();

        var provider = await identityProviders.GetEnabledByAliasAsync(identifier, cancellationToken)
            ?? throw new InvalidOperationException($"Identity provider '{identifier}' was not found or is disabled.");

        var registration = BuildRegistration(provider, configCipher);
        _registrationsByAlias[identifier] = registration;
        return registration;
    }

    public override ValueTask<OpenIddictClientRegistration> GetClientRegistrationByIssuerAsync(
        Uri issuer, CancellationToken cancellationToken = default)
    {
        var match = _registrationsByAlias.Values.FirstOrDefault(registration => registration.Issuer == issuer);
        if (match is not null)
        {
            return ValueTask.FromResult(match);
        }

        throw new InvalidOperationException($"No cached registration found for issuer '{issuer}'. " +
            "The registration must be resolved by alias (GetClientRegistrationByIdAsync) at least once first.");
    }

    private static OpenIddictClientRegistration BuildRegistration(
        Domain.Entities.IdentityProvider provider,
        IIdentityProviderConfigCipher configCipher)
    {
        var config = DeserializeConfig(configCipher.Decrypt(provider.ConfigJson));

        var registration = new OpenIddictClientRegistration
        {
            RegistrationId = provider.Alias,
            ProviderName = provider.Alias,
            ClientId = config.ClientId,
            ClientSecret = config.ClientSecret,
            RedirectUri = new Uri($"/callback/login/{provider.Alias}", UriKind.Relative),
            PostLogoutRedirectUri = new Uri($"/callback/logout/{provider.Alias}", UriKind.Relative)
        };

        switch (provider.ProviderType)
        {
            case IdentityProviderType.Google:
                registration.ProviderType = OpenIddictClientWebIntegrationConstants.ProviderTypes.Google;
                registration.ProviderSettings = new OpenIddictClientWebIntegrationSettings.Google();
                OpenIddictClientWebIntegrationConfiguration.ConfigureProvider(registration);
                break;

            case IdentityProviderType.Microsoft:
                registration.ProviderType = OpenIddictClientWebIntegrationConstants.ProviderTypes.Microsoft;
                registration.ProviderSettings = new OpenIddictClientWebIntegrationSettings.Microsoft();
                OpenIddictClientWebIntegrationConfiguration.ConfigureProvider(registration);
                break;

            case IdentityProviderType.GitHub:
                registration.ProviderType = OpenIddictClientWebIntegrationConstants.ProviderTypes.GitHub;
                registration.ProviderSettings = new OpenIddictClientWebIntegrationSettings.GitHub();
                OpenIddictClientWebIntegrationConfiguration.ConfigureProvider(registration);
                break;

            case IdentityProviderType.GenericOidc:
                var issuer = new Uri(config.Issuer!, UriKind.Absolute);
                registration.Issuer = issuer;
                registration.ConfigurationEndpoint = new Uri(issuer, ".well-known/openid-configuration");
                registration.Scopes.Add(OpenIddictConstants.Scopes.Email);
                registration.Scopes.Add(OpenIddictConstants.Scopes.Profile);
                break;

            default:
                throw new InvalidOperationException(
                    $"Identity provider type '{provider.ProviderType}' does not support federation.");
        }

        return registration;
    }

    private static FederatedProviderConfig DeserializeConfig(string? configJson)
    {
        return JsonSerializer.Deserialize<FederatedProviderConfig>(configJson!, JsonOptions)
            ?? throw new InvalidOperationException("Invalid federated provider configuration.");
    }
}
