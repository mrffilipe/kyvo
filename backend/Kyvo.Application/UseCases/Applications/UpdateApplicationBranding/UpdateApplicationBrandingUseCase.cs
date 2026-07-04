using Kyvo.Application.Exceptions;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Application.UseCases.Applications.UpdateApplicationBranding;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Applications;

public sealed class UpdateApplicationBrandingUseCase : IUpdateApplicationBrandingUseCase
{
    private readonly IApplicationRepository _applications;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateApplicationBrandingUseCase(IApplicationRepository applications, IUnitOfWork unitOfWork)
    {
        _applications = applications;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(UpdateApplicationBrandingRequest request, CancellationToken ct = default)
    {
        EnsurePlatformAdministrator(request.ActorPlatformRoles);

        var application = await _applications.GetForUpdateAsync(request.ApplicationId, ct)
            ?? throw new DomainNotFoundException(ApplicationErrorMessages.Application.NOT_FOUND);

        if (application.IsSystem)
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Application.SYSTEM_APPLICATION_CANNOT_BE_MODIFIED);
        }

        if (request.BrandingEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.BrandingPrimaryColor) ||
                string.IsNullOrWhiteSpace(request.BrandingSecondaryColor))
            {
                throw new DomainValidationException(DomainErrorMessages.Application.BRANDING_COLORS_REQUIRED_WHEN_ENABLED);
            }
        }

        application.UpdateBranding(
            request.BrandingEnabled,
            request.BrandingPrimaryColor,
            request.BrandingSecondaryColor,
            request.BrandingHeroTitle,
            request.BrandingHeroSubtitle);

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static void EnsurePlatformAdministrator(IReadOnlyList<string> actorPlatformRoles)
    {
        if (!actorPlatformRoles.Any(role => PlatformRoleDefaults.AdministrativeKeys.Contains(role)))
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.USER_HAS_NO_TENANT_ACCESS);
        }
    }
}
