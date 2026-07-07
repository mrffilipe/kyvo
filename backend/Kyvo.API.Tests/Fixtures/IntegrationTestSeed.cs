using Kyvo.Domain.Constants;
using Kyvo.Domain.Entities;
using Kyvo.Domain.ValueObjects;
using Kyvo.Infrastructure.Identity;
using Kyvo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kyvo.API.Tests.Fixtures;

internal sealed record IntegrationTestScenario(
    Guid UserId,
    Guid SessionId,
    Guid TenantId,
    Guid MembershipId);

internal static class IntegrationTestSeed
{
    public static async Task<IntegrationTestScenario> SeedTenantMembershipAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var userId = Guid.NewGuid();
        var email = $"user-{userId:N}@integration.test";

        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            DisplayName = "Integration User",
            EmailConfirmed = true,
            IsActive = true,
        });

        var tenant = new Tenant("Integration Tenant", new TenantKey($"it{userId:N}"[..12]));
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var ownerRole = new TenantRole(tenant.Id, new TenantRoleKey(TenantRoleDefaults.OWNER), "Owner", isSystem: true);
        db.TenantRoles.Add(ownerRole);
        await db.SaveChangesAsync();

        var membership = new TenantMembership(tenant.Id, userId, [ownerRole]);
        db.TenantMemberships.Add(membership);

        var session = new AuthSession(userId, null, null, DateTime.UtcNow.AddHours(1), "integration-test", "127.0.0.1");
        db.AuthSessions.Add(session);
        await db.SaveChangesAsync();

        return new IntegrationTestScenario(userId, session.Id, tenant.Id, membership.Id);
    }
}
