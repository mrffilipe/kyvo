using Kyvo.Domain.Entities;
using Kyvo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Persistence.Repositories;

public sealed class ApplicationTenantRepository : IApplicationTenantRepository
{
    private readonly ApplicationDbContext _context;

    public ApplicationTenantRepository(ApplicationDbContext context) => _context = context;

    public Task AddAsync(ApplicationTenant applicationTenant, CancellationToken ct = default)
    {
        return _context.ApplicationTenants
            .AddAsync(applicationTenant, ct)
            .AsTask();
    }

    public Task<ApplicationTenant?> GetByApplicationAndTenantAsync(Guid applicationId, Guid tenantId, CancellationToken ct = default)
    {
        return _context.ApplicationTenants
            .FirstOrDefaultAsync(
                x => x.ApplicationId == applicationId && x.TenantId == tenantId,
                ct);
    }

    public Task<bool> MappingAlreadyExistsAsync(Guid applicationId, Guid tenantId, CancellationToken ct = default)
    {
        return _context.ApplicationTenants
            .AnyAsync(x => x.ApplicationId == applicationId && x.TenantId == tenantId, ct);
    }
}
