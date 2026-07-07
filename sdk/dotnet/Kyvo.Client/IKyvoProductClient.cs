using Kyvo.Client.Models;

namespace Kyvo.Client;

public interface IKyvoProductClient
{
    IKyvoAuthApi Auth { get; }

    IKyvoUsersApi Users { get; }

    IKyvoTenantsApi Tenants { get; }

    IKyvoMembershipsApi Memberships { get; }

    IKyvoTenantRolesApi TenantRoles { get; }

    IKyvoAuditLogsApi AuditLogs { get; }
}

public interface IKyvoAuthApi
{
    Task<SubscribeTenantResult> SubscribeAsync(
        string platformAccessToken,
        SubscribeTenantRequest request,
        CancellationToken cancellationToken = default);

    Task<TenantContextResult> SwitchTenantAsync(
        string platformAccessToken,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuthSessionDto>> ListSessionsAsync(
        string platformAccessToken,
        CancellationToken cancellationToken = default);

    Task RevokeSessionAsync(
        string platformAccessToken,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task DeleteAccountAsync(string tenantAccessToken, CancellationToken cancellationToken = default);
}

public interface IKyvoUsersApi
{
    Task<UserDto> GetMeAsync(string platformAccessToken, CancellationToken cancellationToken = default);

    Task UpdateMeAsync(
        string platformAccessToken,
        UpdateUserProfileBody body,
        CancellationToken cancellationToken = default);

    Task<PagedResult<UserMembershipDto>> ListMyMembershipsAsync(
        string platformAccessToken,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}

public interface IKyvoTenantsApi
{
    Task<PagedResult<TenantDto>> ListAsync(
        string platformAccessToken,
        int page = 1,
        int pageSize = 20,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<TenantDto> GetByIdAsync(
        string tenantAccessToken,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        string tenantAccessToken,
        Guid tenantId,
        UpdateTenantBody body,
        CancellationToken cancellationToken = default);

    Task<InviteMemberResult> InviteMemberAsync(
        string tenantAccessToken,
        Guid tenantId,
        InviteMemberBody body,
        CancellationToken cancellationToken = default);

    Task<PagedResult<TenantInviteDto>> ListInvitesAsync(
        string tenantAccessToken,
        Guid tenantId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task RevokeInviteAsync(
        string tenantAccessToken,
        Guid inviteId,
        CancellationToken cancellationToken = default);

    Task<CreatedMembershipIdResponse> AcceptInviteAsync(
        string platformAccessToken,
        AcceptInviteBody body,
        CancellationToken cancellationToken = default);

    Task<AvailabilityDto> IsKeyAvailableAsync(
        string platformAccessToken,
        string key,
        CancellationToken cancellationToken = default);
}

public interface IKyvoMembershipsApi
{
    Task<CreatedIdResponse> CreateAsync(
        string tenantAccessToken,
        Guid tenantId,
        CreateMembershipBody body,
        CancellationToken cancellationToken = default);

    Task<PagedResult<MembershipDto>> ListByTenantAsync(
        string tenantAccessToken,
        Guid tenantId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task UpdateRolesAsync(
        string tenantAccessToken,
        Guid membershipId,
        UpdateMembershipRolesBody body,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        string tenantAccessToken,
        Guid membershipId,
        CancellationToken cancellationToken = default);
}

public interface IKyvoTenantRolesApi
{
    Task<PagedResult<TenantRoleDto>> ListAsync(
        string tenantAccessToken,
        Guid tenantId,
        bool includeInactive = false,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<CreatedIdResponse> CreateAsync(
        string tenantAccessToken,
        Guid tenantId,
        CreateTenantRoleBody body,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        string tenantAccessToken,
        Guid roleId,
        UpdateTenantRoleBody body,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string tenantAccessToken, Guid roleId, CancellationToken cancellationToken = default);
}

public interface IKyvoAuditLogsApi
{
    Task<PagedResult<AuditLogItemDto>> ListAsync(
        string tenantAccessToken,
        ListAuditLogsQuery? query = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AuditLogFilterOptionDto>> ListFilterOptionsAsync(
        string tenantAccessToken,
        ListAuditLogFilterOptionsQuery query,
        CancellationToken cancellationToken = default);
}
