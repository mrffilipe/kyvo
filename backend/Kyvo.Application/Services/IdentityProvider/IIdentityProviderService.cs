using Kyvo.Application.Common;

namespace Kyvo.Application.Services.IdentityProvider;

public interface IIdentityProviderService
{
    Task<AvailabilityDto> IsAliasAvailableAsync(string alias, CancellationToken cancellationToken = default);
    Task<AddIdentityProviderResult> AddAsync(AddIdentityProviderRequest request, CancellationToken cancellationToken = default);
    Task UpdateIdentityProviderAsync(UpdateIdentityProviderRequest request, CancellationToken cancellationToken = default);
    Task EnableIdentityProviderAsync(Guid id, CancellationToken cancellationToken = default);
    Task DisableIdentityProviderAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IdentityProviderDto?> GetIdentityProviderByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdentityProviderDto>> ListIdentityProvidersAsync(CancellationToken cancellationToken = default);
}
