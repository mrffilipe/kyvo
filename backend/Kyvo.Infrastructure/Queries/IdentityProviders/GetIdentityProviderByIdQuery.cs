using Kyvo.Application.Queries.IdentityProviders.Dtos;
using Kyvo.Application.Queries.IdentityProviders.GetIdentityProviderById;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Repositories;

namespace Kyvo.Infrastructure.Queries.IdentityProviders;

public sealed class GetIdentityProviderByIdQuery : IGetIdentityProviderByIdQuery
{
    private readonly IIdentityProviderRepository _identityProviders;

    public GetIdentityProviderByIdQuery(IIdentityProviderRepository identityProviders) => _identityProviders = identityProviders;

    public async Task<IdentityProviderDto?> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var provider = await _identityProviders.GetForUpdateAsync(id, ct);
        return provider is null ? null : MapToDto(provider);
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
