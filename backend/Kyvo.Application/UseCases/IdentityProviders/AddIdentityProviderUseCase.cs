using Kyvo.Application.Exceptions;
using Kyvo.Application.Services.IdentityProvider;
using Kyvo.Application.UseCases.IdentityProviders.AddIdentityProvider;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.IdentityProviders;

public sealed class AddIdentityProviderUseCase : IAddIdentityProviderUseCase
{
    private static readonly IReadOnlyCollection<IdpCapability> SocialCapabilities =
    [
        IdpCapability.GoogleSocial,
        IdpCapability.MicrosoftSocial,
        IdpCapability.AppleSocial
    ];

    private readonly IIdentityProviderRepository _identityProviders;
    private readonly IIdentityProviderConfigValidator _configValidator;
    private readonly IIdentityProviderConfigCipher _configCipher;
    private readonly IUnitOfWork _unitOfWork;

    public AddIdentityProviderUseCase(
        IIdentityProviderRepository identityProviders,
        IIdentityProviderConfigValidator configValidator,
        IIdentityProviderConfigCipher configCipher,
        IUnitOfWork unitOfWork)
    {
        _identityProviders = identityProviders;
        _configValidator = configValidator;
        _configCipher = configCipher;
        _unitOfWork = unitOfWork;
    }

    public async Task<AddIdentityProviderResult> ExecuteAsync(AddIdentityProviderRequest request, CancellationToken ct = default)
    {
        if (request.ProviderType == IdentityProviderType.Local)
        {
            throw new DomainBusinessRuleException(
                ApplicationErrorMessages.IdentityProvider.LOCAL_PROVIDER_CREATION_NOT_ALLOWED);
        }

        if (await _identityProviders.AliasAlreadyExistsAsync(request.Alias, ct))
        {
            throw new DomainBusinessRuleException(ApplicationErrorMessages.IdentityProvider.ALIAS_ALREADY_EXISTS);
        }

        _configValidator.ValidateForSave(request.ProviderType, request.ConfigJson);
        await EnforceCapabilityUniquenessOnAddAsync(request.Capabilities, ct);

        var encryptedConfig = _configCipher.Encrypt(request.ProviderType, request.ConfigJson);

        var provider = new Domain.Entities.IdentityProvider(
            request.Alias,
            request.DisplayName,
            request.ProviderType,
            request.Capabilities,
            enabled: true,
            configJson: encryptedConfig);

        await _identityProviders.AddAsync(provider, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var warnings = await BuildSocialWarningsAsync(provider.Id, request.Capabilities, ct);
        return new AddIdentityProviderResult { Id = provider.Id, Warnings = warnings };
    }

    private async Task EnforceCapabilityUniquenessOnAddAsync(IReadOnlyCollection<IdpCapability> capabilities, CancellationToken ct)
    {
        if (!capabilities.Contains(IdpCapability.LocalPassword))
        {
            return;
        }

        var existing = await _identityProviders.ListEnabledByCapabilityAsync(IdpCapability.LocalPassword, ct);
        if (existing.Count > 0)
        {
            throw new DomainBusinessRuleException(
                ApplicationErrorMessages.IdentityProviderCapability.LOCAL_PASSWORD_ALREADY_HANDLED);
        }
    }

    private async Task<IReadOnlyList<string>> BuildSocialWarningsAsync(Guid newProviderId, IReadOnlyCollection<IdpCapability> capabilities, CancellationToken ct)
    {
        var warnings = new List<string>();

        foreach (var capability in capabilities.Where(SocialCapabilities.Contains))
        {
            var others = await _identityProviders.ListEnabledByCapabilityAsync(capability, ct);
            var collisions = others.Where(x => x.Id != newProviderId).Select(x => x.Alias).ToList();
            if (collisions.Count > 0)
            {
                warnings.Add(string.Format(
                    ApplicationErrorMessages.IdentityProviderCapability.SOCIAL_ALREADY_HANDLED_FORMAT,
                    capability,
                    string.Join(", ", collisions)));
            }
        }

        return warnings;
    }
}
