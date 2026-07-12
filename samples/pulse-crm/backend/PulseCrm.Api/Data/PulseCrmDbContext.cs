using Kyvo.AspNetCore;
using Microsoft.EntityFrameworkCore;

namespace PulseCrm.Api.Data;

/// <summary>
/// Product DbContext with native EF tenant filters from the Kyvo tenant JWT (<c>tid</c> via <see cref="IKyvoUserContext"/>).
/// </summary>
public sealed class PulseCrmDbContext : DbContext
{
    private readonly IKyvoUserContext _userContext;

    public PulseCrmDbContext(DbContextOptions<PulseCrmDbContext> options, IKyvoUserContext userContext)
        : base(options)
    {
        _userContext = userContext;
    }

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<Contact> Contacts => Set<Contact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.TenantId).IsUnique();
            entity.Property(x => x.CompanyName).HasMaxLength(200);
            entity.Property(x => x.TenantKey).HasMaxLength(80);
            entity.Property(x => x.PlanCode).HasMaxLength(80);
            entity.Property(x => x.ExternalCustomerId).HasMaxLength(120);
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.TenantId);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.HasQueryFilter(c =>
                _userContext.TenantId == null || c.TenantId == _userContext.TenantId);
        });
    }

    public override int SaveChanges()
    {
        StampTenantIds();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTenantIds();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampTenantIds()
    {
        if (!_userContext.TenantId.HasValue)
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                entry.Entity.TenantId = _userContext.TenantId.Value;
            }
        }
    }
}
