using Kyvo.Domain.Common;
using Kyvo.Domain.Entities;
using Kyvo.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TenancyKit.Abstractions;
using TenancyKit.Core;
using TenancyKit.EntityFrameworkCore;

namespace Kyvo.Infrastructure.Persistence;

/// <summary>
/// Extends <see cref="IdentityDbContext{TUser,TRole,TKey}"/> so ASP.NET Core Identity's tables share the same
/// context/migrations as the Kyvo domain and OpenIddict entities.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    private readonly TenancyKitOptions<TenantInfoAdapter> _tenancyOptions;
    private readonly ITenantContextAccessor<TenantInfoAdapter> _tenantContextAccessor;

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
        TenancyKitOptions<TenantInfoAdapter> tenancyOptions,
        ITenantContextAccessor<TenantInfoAdapter> tenantContextAccessor) : base(options)
    {
        _tenancyOptions = tenancyOptions;
        _tenantContextAccessor = tenantContextAccessor;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        modelBuilder.ApplyMultiTenancy(_tenancyOptions, _tenantContextAccessor);
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
