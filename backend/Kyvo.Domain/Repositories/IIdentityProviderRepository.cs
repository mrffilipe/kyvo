using Kyvo.Domain.Entities;
using Kyvo.Domain.Enums;

namespace Kyvo.Domain.Repositories;

public interface IIdentityProviderRepository
{
    Task AddAsync(IdentityProvider provider, CancellationToken ct = default);
    Task<IdentityProvider?> GetByAliasAsync(string alias, CancellationToken ct = default);
    Task<IdentityProvider?> GetEnabledByAliasAsync(string alias, CancellationToken ct = default);
    Task<IdentityProvider?> GetForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<IdentityProvider>> ListEnabledAsync(CancellationToken ct = default);
    Task<IReadOnlyList<IdentityProvider>> ListEnabledByCapabilityAsync(IdpCapability capability, CancellationToken ct = default);
    Task<IReadOnlyList<IdentityProvider>> ListAllAsync(CancellationToken ct = default);
    Task<bool> AliasAlreadyExistsAsync(string alias, CancellationToken ct = default);
}
