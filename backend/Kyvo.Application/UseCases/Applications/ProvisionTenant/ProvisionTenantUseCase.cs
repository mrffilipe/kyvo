using Kyvo.Application.Exceptions;
using Kyvo.Application.Services.TenantResolutionCache;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Application.Shared;
using Kyvo.Application.UseCases.Applications.ProvisionTenant;
using Kyvo.Application.UseCases.Invites.InviteMember;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Applications;

public sealed class ProvisionTenantUseCase : IProvisionTenantUseCase
{
    private readonly ITenantProvisioner _tenantProvisioner;
    private readonly IApplicationRepository _applications;
    private readonly IApplicationTenantRepository _applicationTenants;
    private readonly IInviteMemberUseCase _inviteMemberUseCase;
    private readonly ITenantResolutionCache _tenantResolutionCache;
    private readonly IUnitOfWork _unitOfWork;

    public ProvisionTenantUseCase(
        ITenantProvisioner tenantProvisioner,
        IApplicationRepository applications,
        IApplicationTenantRepository applicationTenants,
        IInviteMemberUseCase inviteMemberUseCase,
        ITenantResolutionCache tenantResolutionCache,
        IUnitOfWork unitOfWork)
    {
        _tenantProvisioner = tenantProvisioner;
        _applications = applications;
        _applicationTenants = applicationTenants;
        _inviteMemberUseCase = inviteMemberUseCase;
        _tenantResolutionCache = tenantResolutionCache;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProvisionApplicationTenantResult> ExecuteAsync(ProvisionApplicationTenantRequest request, CancellationToken ct = default)
    {
        if (!request.ActorPlatformRoles.Any(role => PlatformRoleDefaults.AdministrativeKeys.Contains(role)))
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.USER_HAS_NO_TENANT_ACCESS);
        }

        var application = await _applications.GetByIdAsync(request.ApplicationId, ct)
            ?? throw new DomainNotFoundException(ApplicationErrorMessages.Application.NOT_FOUND);

        var initialAdministratorUserId = request.InitialAdministratorUserId ?? request.ActorUserId;

        var provisionResult = await _tenantProvisioner.ProvisionAsync(
            new TenantProvisionRequest
            {
                TenantName = request.TenantName,
                TenantKey = request.TenantKey,
                OwnerUserId = initialAdministratorUserId
            },
            ct);

        var applicationTenant = new ApplicationTenant(
            application.Id,
            provisionResult.TenantId,
            request.ExternalCustomerId,
            request.PlanCode);

        if (await _applicationTenants.MappingAlreadyExistsAsync(application.Id, provisionResult.TenantId, ct))
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.ApplicationTenant.MAPPING_ALREADY_EXISTS);
        }

        await _applicationTenants.AddAsync(applicationTenant, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await _tenantResolutionCache.InvalidateByIdentifierAsync(provisionResult.TenantKey, ct);

        if (!string.IsNullOrWhiteSpace(request.InitialAdministratorEmail)
            && request.InitialAdministratorUserId is null)
        {
            await _inviteMemberUseCase.ExecuteAsync(
                new InviteMemberRequest
                {
                    TenantId = provisionResult.TenantId,
                    Email = request.InitialAdministratorEmail.Trim(),
                    Roles = [TenantRoleDefaults.OWNER],
                    InvitedByUserId = request.ActorUserId,
                    ActorUserId = request.ActorUserId,
                    ActorPlatformRoles = request.ActorPlatformRoles
                },
                ct);
        }

        return new ProvisionApplicationTenantResult
        {
            ApplicationId = application.Id,
            TenantId = provisionResult.TenantId,
            MembershipId = provisionResult.MembershipId
        };
    }
}
