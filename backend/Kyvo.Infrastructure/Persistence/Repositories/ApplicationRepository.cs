using Kyvo.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using AppEntity = Kyvo.Domain.Entities.Application;

namespace Kyvo.Infrastructure.Persistence.Repositories;

public sealed class ApplicationRepository : IApplicationRepository
{
    private readonly ApplicationDbContext _context;

    public ApplicationRepository(ApplicationDbContext context) => _context = context;

    public Task AddAsync(AppEntity application, CancellationToken ct = default)
    {
        return _context.Applications
            .AddAsync(application, ct)
            .AsTask();
    }

    public Task<AppEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _context.Applications
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<AppEntity?> GetForUpdateAsync(Guid id, CancellationToken ct = default)
    {
        return _context.Applications
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<bool> SlugAlreadyExistsAsync(string slug, CancellationToken ct = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        return _context.Applications
            .AnyAsync(x => x.Slug == normalized, ct);
    }
}
