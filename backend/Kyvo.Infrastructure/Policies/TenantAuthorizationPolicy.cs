using Kyvo.Application.Exceptions;
using Kyvo.Application.Policies;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Repositories;

namespace Kyvo.Infrastructure.Policies;

public sealed class TenantAuthorizationPolicy : ITenantAuthorizationPolicy
{
    private readonly ITenantMembershipRepository _memberships;

    public TenantAuthorizationPolicy(ITenantMembershipRepository memberships)
    {
        _memberships = memberships;
    }

    public async Task EnsureTenantAdministrativeAccessAsync(
        Guid tenantId,
        Guid actorUserId,
        IReadOnlyCollection<string> actorPlatformRoles,
        CancellationToken ct = default)
    {
        if (actorPlatformRoles.Any(role => PlatformRoleDefaults.AdministrativeKeys.Contains(role)))
        {
            return;
        }

        var membership = await _memberships.GetByUserIdAndTenantIdWithRolesAsync(
            actorUserId,
            tenantId,
            ct);

        var hasAdministrativeRole = membership is not null
            && membership.IsActive
            && membership.Roles.Any(role => TenantRoleDefaults.AdministrativeKeys.Contains(role.Role.Key.Value));

        if (!hasAdministrativeRole)
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.USER_HAS_NO_TENANT_ACCESS);
        }
    }
}
