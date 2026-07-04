using Kyvo.Application.Exceptions;
using Kyvo.Application.Ports.Branding;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Application.UseCases.Applications.DeleteApplicationBrandingLogo;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Applications;

public sealed class DeleteApplicationBrandingLogoUseCase : IDeleteApplicationBrandingLogoUseCase
{
    private readonly IApplicationRepository _applications;
    private readonly IApplicationBrandingStorage _brandingStorage;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteApplicationBrandingLogoUseCase(
        IApplicationRepository applications,
        IApplicationBrandingStorage brandingStorage,
        IUnitOfWork unitOfWork)
    {
        _applications = applications;
        _brandingStorage = brandingStorage;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid applicationId, IReadOnlyList<string> actorPlatformRoles, CancellationToken ct = default)
    {
        EnsurePlatformAdministrator(actorPlatformRoles);

        var application = await _applications.GetForUpdateAsync(applicationId, ct)
            ?? throw new DomainNotFoundException(ApplicationErrorMessages.Application.NOT_FOUND);

        if (application.IsSystem)
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Application.SYSTEM_APPLICATION_CANNOT_BE_MODIFIED);
        }

        await _brandingStorage.DeleteLogoAsync(applicationId, ct);
        application.ClearBrandingLogoPath();
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
