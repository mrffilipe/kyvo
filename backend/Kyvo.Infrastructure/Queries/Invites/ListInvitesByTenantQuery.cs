using Kyvo.Application.Common;
using Kyvo.Application.Policies;
using Kyvo.Application.Queries.Invites.Dtos;
using Kyvo.Application.Queries.Invites.ListInvitesByTenant;
using Kyvo.Application.Services.Security;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Repositories;

namespace Kyvo.Infrastructure.Queries.Invites;

public sealed class ListInvitesByTenantQuery : IListInvitesByTenantQuery
{
    private readonly ITenantInviteRepository _invites;
    private readonly IInviteTokenProtector _inviteTokenProtector;
    private readonly ITenantAuthorizationPolicy _authorizationPolicy;

    public ListInvitesByTenantQuery(
        ITenantInviteRepository invites,
        IInviteTokenProtector inviteTokenProtector,
        ITenantAuthorizationPolicy authorizationPolicy)
    {
        _invites = invites;
        _inviteTokenProtector = inviteTokenProtector;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<PagedResult<TenantInviteDto>> ExecuteAsync(ListInvitesByTenantRequest request, CancellationToken ct = default)
    {
        await _authorizationPolicy.EnsureTenantAdministrativeAccessAsync(
            request.TenantId,
            request.ActorUserId,
            request.ActorPlatformRoles,
            ct);

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : request.PageSize;

        var (items, total) = await _invites.ListByTenantIdAsync(
            request.TenantId,
            page,
            pageSize,
            ct);

        var dtos = items.Select(MapInviteToDto).ToList();

        return new PagedResult<TenantInviteDto>
        {
            Items = dtos,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private TenantInviteDto MapInviteToDto(TenantInvite invite)
    {
        var status = invite.GetStatus();
        string? acceptPath = null;

        if (status == TenantInviteStatus.Pending
            && !string.IsNullOrWhiteSpace(invite.EncryptedToken))
        {
            try
            {
                var rawToken = _inviteTokenProtector.Unprotect(invite.EncryptedToken);
                acceptPath = InviteAcceptPath.Build(rawToken);
            }
            catch (InvalidOperationException)
            {
                acceptPath = null;
            }
        }

        return new TenantInviteDto
        {
            Id = invite.Id,
            Email = invite.Email.Value,
            Roles = invite.Roles.Select(x => x.Role.Key.Value).ToList(),
            ExpiresAt = invite.ExpiresAt,
            ConsumedAt = invite.ConsumedAt,
            RevokedAt = invite.RevokedAt,
            Status = status,
            AcceptPath = acceptPath
        };
    }
}
