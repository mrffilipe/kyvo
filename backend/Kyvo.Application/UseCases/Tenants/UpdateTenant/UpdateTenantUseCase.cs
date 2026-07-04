using Kyvo.Application.Exceptions;
using Kyvo.Application.Policies;
using Kyvo.Application.Services.TenantResolutionCache;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Tenants.UpdateTenant;

public sealed class UpdateTenantUseCase : IUpdateTenantUseCase
{
    private readonly ITenantRepository _tenants;
    private readonly ITenantAuthorizationPolicy _authorizationPolicy;
    private readonly ITenantResolutionCache _tenantResolutionCache;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTenantUseCase(
        ITenantRepository tenants,
        ITenantAuthorizationPolicy authorizationPolicy,
        ITenantResolutionCache tenantResolutionCache,
        IUnitOfWork unitOfWork)
    {
        _tenants = tenants;
        _authorizationPolicy = authorizationPolicy;
        _tenantResolutionCache = tenantResolutionCache;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(UpdateTenantRequest request, CancellationToken ct = default)
    {
        await _authorizationPolicy.EnsureTenantAdministrativeAccessAsync(
            request.TenantId,
            request.ActorUserId,
            request.ActorPlatformRoles,
            ct);

        var tenant = await _tenants.GetForUpdateAsync(request.TenantId, ct)
            ?? throw new DomainNotFoundException(DomainErrorMessages.Tenant.TENANT_NOT_FOUND);

        tenant.UpdateName(request.Name);
        await _unitOfWork.SaveChangesAsync(ct);
        await _tenantResolutionCache.InvalidateByIdentifierAsync(tenant.Key.Value, ct);
    }
}
