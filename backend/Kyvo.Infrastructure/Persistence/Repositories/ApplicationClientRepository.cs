using Kyvo.Domain.Entities;
using Kyvo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Persistence.Repositories;

public sealed class ApplicationClientRepository : IApplicationClientRepository
{
    private readonly ApplicationDbContext _context;

    public ApplicationClientRepository(ApplicationDbContext context) => _context = context;

    public Task AddAsync(ApplicationClient client, CancellationToken ct = default)
    {
        return _context.ApplicationClients
            .AddAsync(client, ct)
            .AsTask();
    }

    public Task<ApplicationClient?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _context.ApplicationClients
            .Include(x => x.Application)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<ApplicationClient?> GetByClientIdAsync(string clientId, CancellationToken ct = default)
    {
        return _context.ApplicationClients
            .Include(x => x.Application)
            .FirstOrDefaultAsync(x => x.ClientId == clientId, ct);
    }

    public async Task<IReadOnlyList<ApplicationClient>> ListByApplicationIdAsync(Guid applicationId, CancellationToken ct = default)
    {
        return await _context.ApplicationClients
            .AsNoTracking()
            .Where(x => x.ApplicationId == applicationId)
            .OrderBy(x => x.ClientId)
            .ToListAsync(ct);
    }
}
