using Kyvo.Application.Queries.IdentityProviders.Dtos;
using Kyvo.Application.Queries.IdentityProviders.ListIdentityProviders;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Repositories;

namespace Kyvo.Infrastructure.Queries.IdentityProviders;

public sealed class ListIdentityProvidersQuery : IListIdentityProvidersQuery
{
    private readonly IIdentityProviderRepository _identityProviders;

    public ListIdentityProvidersQuery(IIdentityProviderRepository identityProviders) => _identityProviders = identityProviders;

    public async Task<IReadOnlyList<IdentityProviderDto>> ExecuteAsync(CancellationToken ct = default)
    {
        var providers = await _identityProviders.ListAllAsync(ct);
        return providers.Select(MapToDto).ToList();
    }

    private static IdentityProviderDto MapToDto(Domain.Entities.IdentityProvider provider) =>
        new()
        {
            Id = provider.Id,
            Alias = provider.Alias,
            DisplayName = provider.DisplayName,
            ProviderType = provider.ProviderType,
            Enabled = provider.Enabled,
            Capabilities = (IReadOnlyCollection<IdpCapability>)provider.Capabilities
        };
}
