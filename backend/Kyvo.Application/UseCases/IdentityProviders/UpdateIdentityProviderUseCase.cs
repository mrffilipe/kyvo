using Kyvo.Application.Exceptions;
using Kyvo.Application.Ports.Federation;
using Kyvo.Application.Services.IdentityProvider;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Application.UseCases.IdentityProviders.UpdateIdentityProvider;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.IdentityProviders;

public sealed class UpdateIdentityProviderUseCase : IUpdateIdentityProviderUseCase
{
    private readonly IIdentityProviderRepository _identityProviders;
    private readonly IIdentityProviderConfigValidator _configValidator;
    private readonly IIdentityProviderConfigCipher _configCipher;
    private readonly IFederatedProviderRegistrationCache _registrationCache;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateIdentityProviderUseCase(
        IIdentityProviderRepository identityProviders,
        IIdentityProviderConfigValidator configValidator,
        IIdentityProviderConfigCipher configCipher,
        IFederatedProviderRegistrationCache registrationCache,
        IUnitOfWork unitOfWork)
    {
        _identityProviders = identityProviders;
        _configValidator = configValidator;
        _configCipher = configCipher;
        _registrationCache = registrationCache;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(UpdateIdentityProviderRequest request, CancellationToken ct = default)
    {
        var provider = await _identityProviders.GetForUpdateAsync(request.Id, ct)
            ?? throw new DomainNotFoundException(ApplicationErrorMessages.IdentityProvider.NOT_FOUND);

        if (provider.ProviderType == IdentityProviderType.Local)
        {
            throw new DomainBusinessRuleException(
                ApplicationErrorMessages.IdentityProvider.LOCAL_PROVIDER_MODIFICATION_NOT_ALLOWED);
        }

        provider.UpdateDisplayName(request.DisplayName);

        if (request.Capabilities is not null)
        {
            provider.UpdateCapabilities(request.Capabilities);
        }

        if (request.ConfigJson is not null)
        {
            _configValidator.ValidateForSave(provider.ProviderType, request.ConfigJson);
            var encryptedConfig = _configCipher.Encrypt(provider.ProviderType, request.ConfigJson);
            provider.UpdateConfig(encryptedConfig);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        _registrationCache.Invalidate(provider.Alias);
    }
}
