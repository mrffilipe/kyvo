using Kyvo.Application.Policies;
using Kyvo.Application.Services.TenantRoles;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.TenantRoles.UpdateTenantRole;

public sealed class UpdateTenantRoleUseCase : IUpdateTenantRoleUseCase
{
    private readonly ITenantRoleRepository _roles;
    private readonly ITenantAuthorizationPolicy _authorizationPolicy;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTenantRoleUseCase(
        ITenantRoleRepository roles,
        ITenantAuthorizationPolicy authorizationPolicy,
        IUnitOfWork unitOfWork)
    {
        _roles = roles;
        _authorizationPolicy = authorizationPolicy;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(UpdateTenantRoleRequest request, CancellationToken ct = default)
    {
        var role = await _roles.GetForUpdateAsync(request.RoleId, ct)
            ?? throw new DomainNotFoundException(DomainErrorMessages.TenantRole.ROLE_NOT_FOUND);

        await _authorizationPolicy.EnsureTenantAdministrativeAccessAsync(
            role.TenantId,
            request.ActorUserId,
            request.ActorPlatformRoles,
            ct);

        if (role.IsSystem && !request.IsActive)
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantRole.SYSTEM_ROLE_CANNOT_BE_DEACTIVATED);
        }

        role.UpdateDetails(request.Name, request.Description);
        if (request.IsActive)
        {
            role.Activate();
        }
        else
        {
            role.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
