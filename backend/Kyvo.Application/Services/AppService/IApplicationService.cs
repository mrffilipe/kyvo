using Kyvo.Application.Common;

namespace Kyvo.Application.Services.AppService;

public interface IApplicationService
{
    Task<Guid> CreateAsync(CreateApplicationRequest request, CancellationToken cancellationToken = default);

    Task<AvailabilityDto> IsSlugAvailableAsync(string slug, CancellationToken cancellationToken = default);

    Task<Guid> CreateClientAsync(
        CreateApplicationClientRequest request,
        CancellationToken cancellationToken = default);

    Task<ProvisionApplicationTenantResult> ProvisionTenantAsync(
        ProvisionApplicationTenantRequest request,
        CancellationToken cancellationToken = default);

    Task<ApplicationDto?> GetByIdAsync(
        GetApplicationByIdRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ApplicationDto>> ListAsync(
        ListApplicationsRequest request,
        CancellationToken cancellationToken = default);

    Task<ApplicationBrandingDto?> GetBrandingAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task UpdateBrandingAsync(
        UpdateApplicationBrandingRequest request,
        CancellationToken cancellationToken = default);

    Task<string> UploadBrandingLogoAsync(
        UploadApplicationBrandingLogoRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteBrandingLogoAsync(
        Guid applicationId,
        IReadOnlyList<string> actorPlatformRoles,
        CancellationToken cancellationToken = default);
}
