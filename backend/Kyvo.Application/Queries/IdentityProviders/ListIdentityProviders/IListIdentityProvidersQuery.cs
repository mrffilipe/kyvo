using Kyvo.Application.Queries.IdentityProviders.Dtos;

namespace Kyvo.Application.Queries.IdentityProviders.ListIdentityProviders;

public interface IListIdentityProvidersQuery
{
    Task<IReadOnlyList<IdentityProviderDto>> ExecuteAsync(CancellationToken ct = default);
}
