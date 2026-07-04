using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Infrastructure.Persistence;

namespace Kyvo.Infrastructure.Services.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context) => _context = context;

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
