using Kyvo.Application.Services.IdentityProvider;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Repositories;

namespace Kyvo.API.Services;

/// <summary>
/// Loads enabled identity providers once per scope to avoid concurrent EF commands on the same DbContext
/// (e.g. Blazor account page + child component both calling ListEnabledAsync in parallel).
/// </summary>
public sealed class AccountLoginPageStateProvider : IAccountLoginPageStateProvider
{
    private readonly IIdentityProviderRepository _identityProviders;
    private readonly IIdentityProviderConfigCipher _configCipher;
    private readonly IFederatedConfigBuilder _federatedConfigBuilder;
    private Task<AccountLoginPageState>? _stateTask;

    public AccountLoginPageStateProvider(
        IIdentityProviderRepository identityProviders,
        IIdentityProviderConfigCipher configCipher,
        IFederatedConfigBuilder federatedConfigBuilder)
    {
        _identityProviders = identityProviders;
        _configCipher = configCipher;
        _federatedConfigBuilder = federatedConfigBuilder;
    }

    public Task<AccountLoginPageState> GetStateAsync(CancellationToken cancellationToken = default) =>
        _stateTask ??= BuildStateAsync(cancellationToken);

    private async Task<AccountLoginPageState> BuildStateAsync(CancellationToken cancellationToken)
    {
        var enabled = await _identityProviders.ListEnabledAsync(cancellationToken);

        var showLocal = enabled.Any(p =>
            p.ProviderType == IdentityProviderType.Local && p.Capabilities.Contains(IdpCapability.LocalPassword));

        var federated = enabled
            .Where(p => p.ProviderType != IdentityProviderType.Local)
            .Select(p => new AccountFederatedProviderState
            {
                Alias = p.Alias,
                DisplayName = p.DisplayName,
                ProviderType = p.ProviderType,
                ClientConfig = _federatedConfigBuilder.Build(p.ProviderType, _configCipher.Decrypt(p.ConfigJson))
            })
            .ToList();

        return new AccountLoginPageState
        {
            ShowLocalLogin = showLocal,
            HasFederatedProviders = federated.Count > 0,
            FederatedProviders = federated
        };
    }
}
