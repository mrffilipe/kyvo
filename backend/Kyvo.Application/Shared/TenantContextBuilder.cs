using Kyvo.Application.UseCases.Auth;
using Kyvo.Domain.Entities;

namespace Kyvo.Application.Shared;

public static class TenantContextBuilder
{
    public static TenantContextResult Build(
        User user,
        AuthSession session,
        IReadOnlyList<TenantMembership> memberships,
        IReadOnlyList<string> platformRoles)
    {
        var membership = memberships.FirstOrDefault(x => x.Id == session.MembershipId);
        return new TenantContextResult
        {
            UserId = user.Id,
            Email = user.Email!,
            TenantId = session.TenantId,
            MembershipId = session.MembershipId,
            TenantRoles = membership?.Roles.Select(x => x.Role.Key.Value).ToList() ?? [],
            PlatformRoles = platformRoles,
            Tenants = memberships
                .Select(x => new AuthTenantSummaryDto
                {
                    TenantId = x.TenantId,
                    TenantName = x.Tenant.Name,
                    TenantKey = x.Tenant.Key.Value,
                    Roles = x.Roles.Select(role => role.Role.Key.Value).ToList()
                })
                .ToList()
        };
    }
}
