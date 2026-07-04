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
    private Task<AccountLoginPageState>? _stateTask;

    public AccountLoginPageStateProvider(IIdentityProviderRepository identityProviders) => _identityProviders = identityProviders;

    public Task<AccountLoginPageState> GetStateAsync(CancellationToken ct = default) =>
        _stateTask ??= BuildStateAsync(ct);

    private async Task<AccountLoginPageState> BuildStateAsync(CancellationToken ct)
    {
        var enabled = await _identityProviders.ListEnabledAsync(ct);

        var showLocal = enabled.Any(p =>
            p.ProviderType == IdentityProviderType.Local && p.Capabilities.Contains(IdpCapability.LocalPassword));

        var federated = enabled
            .Where(p => p.ProviderType != IdentityProviderType.Local)
            .Select(p => new AccountFederatedProviderState
            {
                Alias = p.Alias,
                DisplayName = p.DisplayName,
                ProviderType = p.ProviderType
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
