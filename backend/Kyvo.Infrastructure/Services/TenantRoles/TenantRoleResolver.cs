using Kyvo.Application.Services.TenantRoles;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;
using Kyvo.Domain.ValueObjects;

namespace Kyvo.Infrastructure.Services.TenantRoles;

public sealed class TenantRoleResolver : ITenantRoleResolver
{
    private readonly ITenantRoleRepository _roles;

    public TenantRoleResolver(ITenantRoleRepository roles) => _roles = roles;

    public async Task<IReadOnlyList<TenantRole>> ResolveActiveRolesAsync(Guid tenantId, IReadOnlyCollection<string> roleKeys, CancellationToken ct = default)
    {
        if (roleKeys is null || roleKeys.Count == 0)
        {
            throw new DomainValidationException(DomainErrorMessages.TenantRole.AT_LEAST_ONE_ROLE_REQUIRED);
        }

        var keys = roleKeys
            .Select(x => new TenantRoleKey(x))
            .GroupBy(x => x.Value)
            .Select(x => x.First())
            .ToList();

        if (keys.Count != roleKeys.Count)
        {
            throw new DomainValidationException(DomainErrorMessages.TenantRole.DUPLICATE_ROLE);
        }

        var roles = await _roles.ListByTenantIdAndKeysAsync(
            tenantId,
            keys,
            activeOnly: true,
            ct);
        if (roles.Count != keys.Count)
        {
            throw new DomainNotFoundException(DomainErrorMessages.TenantRole.ROLE_NOT_FOUND);
        }

        return keys
            .Select(key => roles.First(role => role.Key.Value == key.Value))
            .ToList();
    }
}
