using Kyvo.Application.Queries.Tenants.GetTenantById;
using Kyvo.Domain.Constants;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Kyvo.Application.Queries.Tenants.Dtos;

namespace Kyvo.Infrastructure.Queries.Tenants;

public sealed class GetTenantByIdQuery : IGetTenantByIdQuery
{
    private readonly ApplicationDbContext _context;

    public GetTenantByIdQuery(ApplicationDbContext context) => _context = context;

    public async Task<TenantDto?> ExecuteAsync(GetTenantByIdRequest request, CancellationToken ct = default)
    {
        var isPlatformAdministrator = request.ActorPlatformRoles
            .Any(role => PlatformRoleDefaults.AdministrativeKeys.Contains(role));

        if (!isPlatformAdministrator)
        {
            var hasAdministrativeMembership = await _context.TenantMemberships
                .AsNoTracking()
                .Where(x => x.UserId == request.ActorUserId && x.TenantId == request.TenantId && x.IsActive)
                .AnyAsync(
                    membership => membership.Roles.Any(
                        role => TenantRoleDefaults.AdministrativeKeys.Contains(role.Role.Key.Value)),
                    ct);

            if (!hasAdministrativeMembership)
            {
                return null;
            }
        }

        return await _context.Tenants
            .AsNoTracking()
            .Where(x => x.Id == request.TenantId)
            .Select(x => new TenantDto
            {
                Id = x.Id,
                Name = x.Name,
                Key = x.Key.Value
            })
            .FirstOrDefaultAsync(ct);
    }
}
