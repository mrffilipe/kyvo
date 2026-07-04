using Kyvo.Application.Policies;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.TenantRoles.DeleteTenantRole;

public sealed class DeleteTenantRoleUseCase : IDeleteTenantRoleUseCase
{
    private readonly ITenantRoleRepository _roles;
    private readonly ITenantAuthorizationPolicy _authorizationPolicy;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTenantRoleUseCase(
        ITenantRoleRepository roles,
        ITenantAuthorizationPolicy authorizationPolicy,
        IUnitOfWork unitOfWork)
    {
        _roles = roles;
        _authorizationPolicy = authorizationPolicy;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(DeleteTenantRoleRequest request, CancellationToken ct = default)
    {
        var role = await _roles.GetForUpdateAsync(request.RoleId, ct)
            ?? throw new DomainNotFoundException(DomainErrorMessages.TenantRole.ROLE_NOT_FOUND);

        await _authorizationPolicy.EnsureTenantAdministrativeAccessAsync(
            role.TenantId,
            request.ActorUserId,
            request.ActorPlatformRoles,
            ct);

        if (role.IsSystem)
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantRole.SYSTEM_ROLE_CANNOT_BE_DELETED);
        }

        if (await _roles.HasActiveAssignmentsAsync(role.Id, ct))
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantRole.ROLE_HAS_ACTIVE_ASSIGNMENTS);
        }

        await _roles.DeleteAsync(role, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
