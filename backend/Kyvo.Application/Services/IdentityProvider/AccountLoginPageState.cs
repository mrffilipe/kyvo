namespace Kyvo.Application.Services.IdentityProvider;

/// <summary>
/// Precomputed account login/register UI state (one DB round-trip per HTTP request).
/// </summary>
public sealed class AccountLoginPageState
{
    public required bool ShowLocalLogin { get; init; }

    public required bool HasFederatedProviders { get; init; }

    public required IReadOnlyList<AccountFederatedProviderState> FederatedProviders { get; init; }
}
