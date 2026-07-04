using Kyvo.Application.Exceptions;
using Kyvo.Application.Ports.Federation;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Application.UseCases.IdentityProviders.EnableIdentityProvider;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.IdentityProviders;

public sealed class EnableIdentityProviderUseCase : IEnableIdentityProviderUseCase
{
    private readonly IIdentityProviderRepository _identityProviders;
    private readonly IFederatedProviderRegistrationCache _registrationCache;
    private readonly IUnitOfWork _unitOfWork;

    public EnableIdentityProviderUseCase(
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

        if (provider.Capabilities.Contains(IdpCapability.LocalPassword))
        {
            var conflicts = await _identityProviders.ListEnabledByCapabilityAsync(IdpCapability.LocalPassword, ct);
            if (conflicts.Any(x => x.Id != provider.Id))
            {
                throw new DomainBusinessRuleException(
                    ApplicationErrorMessages.IdentityProviderCapability.LOCAL_PASSWORD_ALREADY_HANDLED);
            }
        }

        provider.Enable();
        await _unitOfWork.SaveChangesAsync(ct);

        if (provider.ProviderType != IdentityProviderType.Local)
        {
            _registrationCache.Invalidate(provider.Alias);
        }
    }
}
