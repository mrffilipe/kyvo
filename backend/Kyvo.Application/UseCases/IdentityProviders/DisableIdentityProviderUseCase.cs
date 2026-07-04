using Kyvo.Application.Exceptions;
using Kyvo.Application.Ports.Federation;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Application.UseCases.IdentityProviders.DisableIdentityProvider;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.IdentityProviders;

public sealed class DisableIdentityProviderUseCase : IDisableIdentityProviderUseCase
{
    private readonly IIdentityProviderRepository _identityProviders;
    private readonly IFederatedProviderRegistrationCache _registrationCache;
    private readonly IUnitOfWork _unitOfWork;

    public DisableIdentityProviderUseCase(
        IIdentityProviderRepository identityProviders,
        IFederatedProviderRegistrationCache registrationCache,
        IUnitOfWork unitOfWork)
    {
        _identityProviders = identityProviders;
        _registrationCache = registrationCache;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var provider = await _identityProviders.GetForUpdateAsync(id, ct)
            ?? throw new DomainNotFoundException(ApplicationErrorMessages.IdentityProvider.NOT_FOUND);

        if (provider.ProviderType == IdentityProviderType.Local)
        {
            throw new DomainBusinessRuleException(
                ApplicationErrorMessages.IdentityProvider.LOCAL_PROVIDER_DISABLE_NOT_ALLOWED);
        }

        provider.Disable();
        await _unitOfWork.SaveChangesAsync(ct);
        _registrationCache.Invalidate(provider.Alias);
    }
}
