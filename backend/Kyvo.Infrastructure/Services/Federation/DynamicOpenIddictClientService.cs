using System.Collections.Concurrent;
using System.Text.Json;
using Kyvo.Domain.Repositories;
using Kyvo.Application.Security;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Client;
using OpenIddict.Client.WebIntegration;

namespace Kyvo.Infrastructure.Services.Federation;

public sealed class DynamicOpenIddictClientService : OpenIddictClientService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IServiceProvider _provider;
    private readonly ConcurrentDictionary<string, OpenIddictClientRegistration> _registrationsByAlias = new(StringComparer.OrdinalIgnoreCase);

    public DynamicOpenIddictClientService(IServiceProvider provider) : base(provider)
    {
        _provider = provider;
    }

    public void Invalidate(string alias) => _registrationsByAlias.TryRemove(alias, out _);

    public override async ValueTask<OpenIddictClientRegistration> GetClientRegistrationByIdAsync(string identifier, CancellationToken cancellationToken = default)
    {
        if (_registrationsByAlias.TryGetValue(identifier, out var cached))
            return cached;

        using var scope = _provider.CreateScope();
        var identityProviders = scope.ServiceProvider.GetRequiredService<IIdentityProviderRepository>();
        var configCipher = scope.ServiceProvider.GetRequiredService<IIdentityProviderConfigCipher>();

        var provider = await identityProviders.GetEnabledByAliasAsync(identifier, cancellationToken)
            ?? throw new InvalidOperationException($"Identity provider '{identifier}' was not found or is disabled.");

        var registration = BuildRegistration(provider, configCipher);
        _registrationsByAlias[identifier] = registration;
        return registration;
    }

    public override ValueTask<OpenIddictClientRegistration> GetClientRegistrationByIssuerAsync(Uri issuer, CancellationToken cancellationToken = default)
    {
        var match = _registrationsByAlias.Values.FirstOrDefault(registration => registration.Issuer == issuer);
        if (match is not null)
            return ValueTask.FromResult(match);

        throw new InvalidOperationException($"No cached registration found for issuer '{issuer}'.");
    }

    private static OpenIddictClientRegistration BuildRegistration(Domain.Entities.IdentityProvider provider, IIdentityProviderConfigCipher configCipher)
    {
        var config = JsonSerializer.Deserialize<FederatedProviderConfig>(configCipher.Decrypt(provider.ConfigJson) ?? "{}", JsonOptions)
            ?? new FederatedProviderConfig();

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
                if (string.IsNullOrEmpty(config.Issuer)) throw new InvalidOperationException("Issuer is required for GenericOidc.");
                var issuer = new Uri(config.Issuer, UriKind.Absolute);
                registration.Issuer = issuer;
                registration.ConfigurationEndpoint = new Uri(issuer, ".well-known/openid-configuration");
                registration.Scopes.Add(OpenIddictConstants.Scopes.Email);
                registration.Scopes.Add(OpenIddictConstants.Scopes.Profile);
                break;
            default:
                throw new InvalidOperationException($"Identity provider type '{provider.ProviderType}' does not support federation.");
        }

        return registration;
    }
}
