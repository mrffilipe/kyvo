using Kyvo.Application.Services.TenantResolutionCache;
using Kyvo.Application.Shared;
using Kyvo.Application.UseCases.Invites.InviteMember;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Tenants.CreateTenant;

public sealed class CreateTenantUseCase : ICreateTenantUseCase
{
    private readonly ITenantProvisioner _tenantProvisioner;
    private readonly IInviteMemberUseCase _inviteMemberUseCase;
    private readonly ITenantResolutionCache _tenantResolutionCache;

    public CreateTenantUseCase(
        ITenantProvisioner tenantProvisioner,
        IInviteMemberUseCase inviteMemberUseCase,
        ITenantResolutionCache tenantResolutionCache)
    {
        _tenantProvisioner = tenantProvisioner;
        _inviteMemberUseCase = inviteMemberUseCase;
        _tenantResolutionCache = tenantResolutionCache;
    }

    public async Task<Guid> ExecuteAsync(CreateTenantRequest request, CancellationToken ct = default)
    {
        var initialAdministratorUserId = request.InitialAdministratorUserId ?? request.ActorUserId;

        var result = await _tenantProvisioner.ProvisionAsync(
            new TenantProvisionRequest
            {
                TenantName = request.Name,
                TenantKey = request.Key,
                OwnerUserId = initialAdministratorUserId
            },
            ct);

        await _tenantResolutionCache.InvalidateByIdentifierAsync(result.TenantKey, ct);

        if (!string.IsNullOrWhiteSpace(request.InitialAdministratorEmail)
            && request.InitialAdministratorUserId is null)
        {
            await _inviteMemberUseCase.ExecuteAsync(
                new InviteMemberRequest
                {
                    TenantId = result.TenantId,
                    Email = request.InitialAdministratorEmail.Trim(),
                    Roles = [TenantRoleDefaults.OWNER],
                    InvitedByUserId = request.ActorUserId,
                    ActorUserId = request.ActorUserId,
                    ActorPlatformRoles = request.ActorPlatformRoles
                },
                ct);
        }

        return result.TenantId;
    }
}
