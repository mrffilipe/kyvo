using Kyvo.Domain.Enums;

namespace Kyvo.Application.Services.IdentityProvider;

public sealed class AccountFederatedProviderState
{
    public required string Alias { get; init; }

    public required string DisplayName { get; init; }

    public required IdentityProviderType ProviderType { get; init; }

    public IReadOnlyDictionary<string, string>? ClientConfig { get; init; }
}
