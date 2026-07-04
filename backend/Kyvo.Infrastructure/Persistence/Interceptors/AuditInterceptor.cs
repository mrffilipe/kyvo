using Kyvo.Application.Services.UserScope;
using Kyvo.Domain.Common;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Enums;
using Kyvo.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kyvo.Infrastructure.Persistence.Interceptors;

public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IUserScope _userScope;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditInterceptor(IUserScope userScope, IHttpContextAccessor httpContextAccessor)
    {
        _userScope = userScope;
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        AddAuditLogs(eventData.Context);
        return base.SavingChangesAsync(
            eventData,
            result,
            ct);
    }

    private void AddAuditLogs(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var logs = BuildAuditLogs(context.ChangeTracker);
        if (logs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(logs);
        }
    }

    private List<AuditLog> BuildAuditLogs(ChangeTracker changeTracker)
    {
        var logs = new List<AuditLog>();

        var entries = changeTracker.Entries<BaseEntity>()
            .Where(x => x.Entity is not AuditLog)
            .Where(x => x.State is EntityState.Added or EntityState.Modified)
            .Select(x => (Entity: (object)x.Entity, x.State, Id: x.Entity.Id))
            .Concat(changeTracker.Entries<ApplicationUser>()
                .Where(x => x.State is EntityState.Added or EntityState.Modified)
                .Select(x => (Entity: (object)x.Entity, x.State, Id: x.Entity.Id)))
            .ToList();

        foreach (var entry in entries)
        {
            var action = ResolveAction(entry.Entity, entry.State);
            if (action is null)
            {
                continue;
            }

            var tenantId = ResolveTenantId(entry.Entity);
            if (!tenantId.HasValue || tenantId.Value == Guid.Empty)
            {
                continue;
            }

            logs.Add(new AuditLog(
                tenantId.Value,
                _userScope.UserId == Guid.Empty ? null : _userScope.UserId,
                _userScope.MembershipId,
                action.Value.ToString(),
                entry.Entity.GetType().Name,
                entry.Id,
                _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString()));
        }

        return logs;
    }

    private static AuditAction? ResolveAction(object entity, EntityState state)
    {
        return entity switch
        {
            ApplicationUser when state == EntityState.Added => AuditAction.UserCreated,
            ApplicationUser when state == EntityState.Modified => AuditAction.UserUpdated,
            Tenant when state == EntityState.Added => AuditAction.TenantCreated,
            Tenant when state == EntityState.Modified => AuditAction.TenantUpdated,
            TenantMembership when state == EntityState.Added => AuditAction.MembershipCreated,
            TenantMembership membership when state == EntityState.Modified && !membership.IsActive => AuditAction.MembershipRevoked,
            TenantMembership when state == EntityState.Modified => AuditAction.MembershipRoleUpdated,
            TenantMembershipRole when state == EntityState.Added => AuditAction.MembershipRoleUpdated,
            AuthSession when state == EntityState.Added => AuditAction.SessionCreated,
            AuthSession session when state == EntityState.Modified && session.Status == SessionStatus.Revoked => AuditAction.SessionRevoked,
            TenantInvite when state == EntityState.Added => AuditAction.InviteCreated,
            TenantInvite invite when state == EntityState.Modified && invite.ConsumedAt.HasValue => AuditAction.InviteAccepted,
            TenantInvite invite when state == EntityState.Modified && invite.RevokedAt.HasValue => AuditAction.InviteRevoked,
            _ => null
        };
    }

    private Guid? ResolveTenantId(object entity)
    {
        return entity switch
        {
            TenantEntity scoped => scoped.TenantId,
            Tenant tenant => tenant.Id,
            AuthSession session => session.TenantId ?? _userScope.TenantId,
            ApplicationUser => _userScope.TenantId,
            _ => _userScope.TenantId
        };
    }
}
