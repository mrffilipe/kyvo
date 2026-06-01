using Kyvo.Domain.Enums;

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

public sealed class AccountFederatedProviderState
{
    public required string Alias { get; init; }

    public required string DisplayName { get; init; }

    public required IdentityProviderType ProviderType { get; init; }

    public IReadOnlyDictionary<string, string>? ClientConfig { get; init; }
}
