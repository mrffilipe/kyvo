using Kyvo.Application.Exceptions;
using Kyvo.Application.Ports.Branding;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Application.UseCases.Applications.UploadApplicationBrandingLogo;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Applications;

public sealed class UploadApplicationBrandingLogoUseCase : IUploadApplicationBrandingLogoUseCase
{
    private readonly IApplicationRepository _applications;
    private readonly IApplicationBrandingStorage _brandingStorage;
    private readonly IUnitOfWork _unitOfWork;

    public UploadApplicationBrandingLogoUseCase(
        IApplicationRepository applications,
        IApplicationBrandingStorage brandingStorage,
        IUnitOfWork unitOfWork)
    {
        _applications = applications;
        _brandingStorage = brandingStorage;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> ExecuteAsync(UploadApplicationBrandingLogoRequest request, CancellationToken ct = default)
    {
        EnsurePlatformAdministrator(request.ActorPlatformRoles);

        var application = await _applications.GetForUpdateAsync(request.ApplicationId, ct)
            ?? throw new DomainNotFoundException(ApplicationErrorMessages.Application.NOT_FOUND);

        if (application.IsSystem)
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Application.SYSTEM_APPLICATION_CANNOT_BE_MODIFIED);
        }

        var logoPath = await _brandingStorage.SaveLogoAsync(
            application.Id,
            request.Content,
            request.ContentType,
            request.FileName,
            ct);

        application.SetBrandingLogoPath(logoPath);
        await _unitOfWork.SaveChangesAsync(ct);
        return logoPath;
    }

    private static void EnsurePlatformAdministrator(IReadOnlyList<string> actorPlatformRoles)
    {
        if (!actorPlatformRoles.Any(role => PlatformRoleDefaults.AdministrativeKeys.Contains(role)))
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.USER_HAS_NO_TENANT_ACCESS);
        }
    }
}
