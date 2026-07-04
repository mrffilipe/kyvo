using Kyvo.Application.Queries.IdentityProviders.Dtos;

namespace Kyvo.Application.Queries.IdentityProviders.GetIdentityProviderById;

public interface IGetIdentityProviderByIdQuery
{
    Task<IdentityProviderDto?> ExecuteAsync(Guid id, CancellationToken ct = default);
}
