using Kyvo.Application.Common;

namespace Kyvo.Application.Services.IdentityProvider;

public interface IIdentityProviderService
{
    Task<AvailabilityDto> IsAliasAvailableAsync(string alias, CancellationToken cancellationToken = default);

    Task<AddIdentityProviderResult> AddAsync(AddIdentityProviderRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(UpdateIdentityProviderRequest request, CancellationToken cancellationToken = default);

    Task EnableAsync(Guid id, CancellationToken cancellationToken = default);

    Task DisableAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IdentityProviderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IdentityProviderDto>> ListAsync(CancellationToken cancellationToken = default);
}
