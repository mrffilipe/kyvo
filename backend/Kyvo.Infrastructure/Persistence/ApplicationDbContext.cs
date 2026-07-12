using Kyvo.Application.Services.Tenancy;
using Kyvo.Domain.Common;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Interfaces;
using Kyvo.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Persistence;

/// <summary>
/// Shared Identity + domain + OpenIddict context with native tenant query filters (no TenancyKit).
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    private readonly ITenantContext _tenantContext;

    public DbSet<UserPlatformRole> UserPlatformRoles { get; private set; } = null!;
    public DbSet<PlatformRole> PlatformRoles { get; private set; } = null!;
    public DbSet<IdentityProvider> IdentityProviders { get; private set; } = null!;
    public DbSet<Tenant> Tenants { get; private set; } = null!;
    public DbSet<TenantRole> TenantRoles { get; private set; } = null!;
    public DbSet<TenantMembership> TenantMemberships { get; private set; } = null!;
    public DbSet<TenantMembershipRole> TenantMembershipRoles { get; private set; } = null!;
    public DbSet<Domain.Entities.Application> Applications { get; private set; } = null!;
    public DbSet<ApplicationTenant> ApplicationTenants { get; private set; } = null!;
    public DbSet<AuthSession> AuthSessions { get; private set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; private set; } = null!;
    public DbSet<TenantInvite> TenantInvites { get; private set; } = null!;
    public DbSet<TenantInviteRole> TenantInviteRoles { get; private set; } = null!;
    public DbSet<PlatformConfiguration> PlatformConfigurations { get; private set; } = null!;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        ApplyNativeTenantFilters(modelBuilder);
    }

    private void ApplyNativeTenantFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var method = typeof(ApplicationDbContext)
                .GetMethod(nameof(SetTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .MakeGenericMethod(entityType.ClrType);
            method.Invoke(this, [modelBuilder]);
        }
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantScoped
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            _tenantContext.TenantId == null || e.TenantId == _tenantContext.TenantId);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.SetCreatedAt();
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.SetUpdatedAt();
            }
        }

        foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.SetCreatedAt();
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.SetUpdatedAt();
            }
        }

        return await base.SaveChangesAsync(ct);
    }
}
