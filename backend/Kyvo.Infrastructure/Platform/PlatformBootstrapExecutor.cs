using Kyvo.Application.Ports.Platform;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Platform;

public sealed class PlatformBootstrapExecutor : IPlatformBootstrapExecutor
{
    private readonly ApplicationDbContext _context;

    public PlatformBootstrapExecutor(ApplicationDbContext context) => _context = context;

    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        await _context.Database.ExecuteSqlRawAsync("SET TRANSACTION ISOLATION LEVEL SERIALIZABLE", ct);

        await operation(ct);
        await transaction.CommitAsync(ct);
    }
}
