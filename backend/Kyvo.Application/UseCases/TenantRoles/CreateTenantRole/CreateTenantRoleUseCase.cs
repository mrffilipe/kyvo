using Kyvo.Application.Policies;
using Kyvo.Application.Services.TenantRoles;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;
using Kyvo.Domain.ValueObjects;

namespace Kyvo.Application.UseCases.TenantRoles.CreateTenantRole;

public sealed class CreateTenantRoleUseCase : ICreateTenantRoleUseCase
{
    private readonly ITenantRepository _tenants;
    private readonly ITenantRoleRepository _roles;
    private readonly ITenantAuthorizationPolicy _authorizationPolicy;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTenantRoleUseCase(
        ITenantRepository tenants,
        ITenantRoleRepository roles,
        ITenantAuthorizationPolicy authorizationPolicy,
        IUnitOfWork unitOfWork)
    {
        _tenants = tenants;
        _roles = roles;
        _authorizationPolicy = authorizationPolicy;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> ExecuteAsync(CreateTenantRoleRequest request, CancellationToken ct = default)
    {
        await _authorizationPolicy.EnsureTenantAdministrativeAccessAsync(
            request.TenantId,
            request.ActorUserId,
            request.ActorPlatformRoles,
            ct);

        _ = await _tenants.GetForUpdateAsync(request.TenantId, ct)
            ?? throw new DomainNotFoundException(DomainErrorMessages.Tenant.TENANT_NOT_FOUND);

        var key = new TenantRoleKey(request.Key);
        if (await _roles.KeyAlreadyExistsAsync(request.TenantId, key, ct))
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantRole.ROLE_ALREADY_EXISTS);
        }

        var role = new Domain.Entities.TenantRole(request.TenantId, key, request.Name, request.Description);
        await _roles.AddAsync(role, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return role.Id;
    }
}
