using System.Net.Http.Headers;
using System.Net.Http.Json;
using Kyvo.Client.Internal;
using Kyvo.Client.Models;
using Microsoft.Extensions.Options;

namespace Kyvo.Client;

public sealed class KyvoProductClient : IKyvoProductClient
{
    private readonly HttpClient _http;
    private readonly KyvoClientOptions _options;

    public KyvoProductClient(HttpClient http, IOptions<KyvoClientOptions> options)
    {
        _http = http;
        _options = options.Value;
        Auth = new AuthApi(this);
        Users = new UsersApi(this);
        Tenants = new TenantsApi(this);
        Memberships = new MembershipsApi(this);
        TenantRoles = new TenantRolesApi(this);
        AuditLogs = new AuditLogsApi(this);
    }

    public IKyvoAuthApi Auth { get; }
    public IKyvoUsersApi Users { get; }
    public IKyvoTenantsApi Tenants { get; }
    public IKyvoMembershipsApi Memberships { get; }
    public IKyvoTenantRolesApi TenantRoles { get; }
    public IKyvoAuditLogsApi AuditLogs { get; }

    private string V => _options.VersionPrefix;

    private async Task<HttpResponseMessage> SendAsync(
        string accessToken,
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(method, path);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            message.Content = JsonContent.Create(body, options: KyvoHttpResponse.SerializerOptions);
        }

        return await _http.SendAsync(message, cancellationToken);
    }

    private sealed class AuthApi(KyvoProductClient client) : IKyvoAuthApi
    {
        public async Task<SubscribeTenantResult> SubscribeAsync(
            string userAccessToken,
            SubscribeTenantRequest request,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Post,
                $"{client.V}/auth/subscribe",
                request,
                cancellationToken);

            var data = await KyvoHttpResponse.ReadJsonAsync<SubscribeTenantResponse>(response, cancellationToken)
                ?? throw new InvalidOperationException("Kyvo subscribe returned empty body.");

            var context = new TenantContextResult(
                data.UserId,
                data.Email,
                data.TenantId,
                data.MembershipId,
                data.TenantRoles,
                data.PlatformRoles,
                data.Tenants);

            return new SubscribeTenantResult(context, data.Tokens);
        }

        public async Task<TenantContextResult> SwitchTenantAsync(
            string userAccessToken,
            Guid tenantId,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Post,
                $"{client.V}/auth/switch-tenant",
                new SwitchTenantRequest(tenantId),
                cancellationToken);

            return await KyvoHttpResponse.ReadJsonAsync<TenantContextResult>(response, cancellationToken)
                ?? throw new InvalidOperationException("Kyvo switch-tenant returned empty body.");
        }

        public async Task<IReadOnlyList<AuthSessionDto>> ListSessionsAsync(
            string userAccessToken,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Get,
                $"{client.V}/auth/sessions",
                null,
                cancellationToken);

            return await KyvoHttpResponse.ReadJsonAsync<List<AuthSessionDto>>(response, cancellationToken) ?? [];
        }

        public async Task RevokeSessionAsync(
            string userAccessToken,
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Delete,
                $"{client.V}/auth/sessions/{sessionId}",
                null,
                cancellationToken);

            await KyvoHttpResponse.EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task DeleteAccountAsync(string userAccessToken, CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Delete,
                $"{client.V}/auth/account",
                null,
                cancellationToken);

            await KyvoHttpResponse.EnsureSuccessAsync(response, cancellationToken);
        }
    }

    private sealed class UsersApi(KyvoProductClient client) : IKyvoUsersApi
    {
        public async Task<UserDto> GetMeAsync(string userAccessToken, CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(userAccessToken, HttpMethod.Get, $"{client.V}/Users/me", null, cancellationToken);
            return await KyvoHttpResponse.ReadJsonAsync<UserDto>(response, cancellationToken)
                ?? throw new InvalidOperationException("Kyvo Users/me returned empty body.");
        }

        public async Task UpdateMeAsync(
            string userAccessToken,
            UpdateUserProfileBody body,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(userAccessToken, HttpMethod.Patch, $"{client.V}/Users/me", body, cancellationToken);
            await KyvoHttpResponse.EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task<PagedResult<UserMembershipDto>> ListMyMembershipsAsync(
            string userAccessToken,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Get,
                $"{client.V}/Users/me/memberships?page={page}&pageSize={pageSize}",
                null,
                cancellationToken);

            return await KyvoHttpResponse.ReadJsonAsync<PagedResult<UserMembershipDto>>(response, cancellationToken)
                ?? new PagedResult<UserMembershipDto>([], 0, page, pageSize);
        }
    }

    private sealed class TenantsApi(KyvoProductClient client) : IKyvoTenantsApi
    {
        public async Task<PagedResult<TenantDto>> ListAsync(
            string userAccessToken,
            int page = 1,
            int pageSize = 20,
            string? search = null,
            CancellationToken cancellationToken = default)
        {
            var qs = new List<string> { $"page={page}", $"pageSize={pageSize}" };
            if (!string.IsNullOrWhiteSpace(search))
            {
                qs.Add($"search={Uri.EscapeDataString(search)}");
            }

            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Get,
                $"{client.V}/Tenants?{string.Join("&", qs)}",
                null,
                cancellationToken);

            return await KyvoHttpResponse.ReadJsonAsync<PagedResult<TenantDto>>(response, cancellationToken)
                ?? new PagedResult<TenantDto>([], 0, page, pageSize);
        }

        public async Task<TenantDto> GetByIdAsync(
            string userAccessToken,
            Guid tenantId,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Get,
                $"{client.V}/Tenants/{tenantId}",
                null,
                cancellationToken);

            return await KyvoHttpResponse.ReadJsonAsync<TenantDto>(response, cancellationToken)
                ?? throw new InvalidOperationException("Kyvo tenant not found.");
        }

        public async Task UpdateAsync(
            string userAccessToken,
            Guid tenantId,
            UpdateTenantBody body,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Patch,
                $"{client.V}/Tenants/{tenantId}",
                body,
                cancellationToken);

            await KyvoHttpResponse.EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task<InviteMemberResult> InviteMemberAsync(
            string userAccessToken,
            Guid tenantId,
            InviteMemberBody body,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Post,
                $"{client.V}/Tenants/{tenantId}/invites",
                body,
                cancellationToken);

            return await KyvoHttpResponse.ReadJsonAsync<InviteMemberResult>(response, cancellationToken)
                ?? throw new InvalidOperationException("Kyvo invite returned empty body.");
        }

        public async Task<PagedResult<TenantInviteDto>> ListInvitesAsync(
            string userAccessToken,
            Guid tenantId,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Get,
                $"{client.V}/Tenants/{tenantId}/invites?page={page}&pageSize={pageSize}",
                null,
                cancellationToken);

            return await KyvoHttpResponse.ReadJsonAsync<PagedResult<TenantInviteDto>>(response, cancellationToken)
                ?? throw new InvalidOperationException("Kyvo list invites returned empty body.");
        }

        public async Task RevokeInviteAsync(
            string userAccessToken,
            Guid inviteId,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Delete,
                $"{client.V}/Invites/{inviteId}",
                null,
                cancellationToken);

            await KyvoHttpResponse.EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task<CreatedMembershipIdResponse> AcceptInviteAsync(
            string userAccessToken,
            AcceptInviteBody body,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Post,
                $"{client.V}/invites/accept",
                body,
                cancellationToken);

            return await KyvoHttpResponse.ReadJsonAsync<CreatedMembershipIdResponse>(response, cancellationToken)
                ?? throw new InvalidOperationException("Kyvo accept invite returned empty body.");
        }

        public async Task<AvailabilityDto> IsKeyAvailableAsync(
            string userAccessToken,
            string key,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Get,
                $"{client.V}/Tenants/keys/{Uri.EscapeDataString(key)}/availability",
                null,
                cancellationToken);

            return await KyvoHttpResponse.ReadJsonAsync<AvailabilityDto>(response, cancellationToken)
                ?? throw new InvalidOperationException("Kyvo tenant key availability returned empty body.");
        }
    }

    private sealed class MembershipsApi(KyvoProductClient client) : IKyvoMembershipsApi
    {
        public async Task<CreatedIdResponse> CreateAsync(
            string userAccessToken,
            Guid tenantId,
            CreateMembershipBody body,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Post,
                $"{client.V}/tenants/{tenantId}/memberships",
                body,
                cancellationToken);

            return await KyvoHttpResponse.ReadJsonAsync<CreatedIdResponse>(response, cancellationToken)
                ?? throw new InvalidOperationException("Kyvo create membership returned empty body.");
        }

        public async Task<PagedResult<MembershipDto>> ListByTenantAsync(
            string userAccessToken,
            Guid tenantId,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Get,
                $"{client.V}/tenants/{tenantId}/memberships?page={page}&pageSize={pageSize}",
                null,
                cancellationToken);

            return await KyvoHttpResponse.ReadJsonAsync<PagedResult<MembershipDto>>(response, cancellationToken)
                ?? new PagedResult<MembershipDto>([], 0, page, pageSize);
        }

        public async Task UpdateRolesAsync(
            string userAccessToken,
            Guid membershipId,
            UpdateMembershipRolesBody body,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Patch,
                $"{client.V}/Memberships/{membershipId}",
                body,
                cancellationToken);

            await KyvoHttpResponse.EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task RevokeAsync(
            string userAccessToken,
            Guid membershipId,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Delete,
                $"{client.V}/Memberships/{membershipId}",
                null,
                cancellationToken);

            await KyvoHttpResponse.EnsureSuccessAsync(response, cancellationToken);
        }
    }

    private sealed class TenantRolesApi(KyvoProductClient client) : IKyvoTenantRolesApi
    {
        public async Task<PagedResult<TenantRoleDto>> ListAsync(
            string userAccessToken,
            Guid tenantId,
            bool includeInactive = false,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Get,
                $"{client.V}/tenants/{tenantId}/roles?includeInactive={includeInactive}&page={page}&pageSize={pageSize}",
                null,
                cancellationToken);

            return await KyvoHttpResponse.ReadJsonAsync<PagedResult<TenantRoleDto>>(response, cancellationToken)
                ?? new PagedResult<TenantRoleDto>([], 0, page, pageSize);
        }

        public async Task<CreatedIdResponse> CreateAsync(
            string userAccessToken,
            Guid tenantId,
            CreateTenantRoleBody body,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Post,
                $"{client.V}/tenants/{tenantId}/roles",
                body,
                cancellationToken);

            return await KyvoHttpResponse.ReadJsonAsync<CreatedIdResponse>(response, cancellationToken)
                ?? throw new InvalidOperationException("Kyvo create role returned empty body.");
        }

        public async Task UpdateAsync(
            string userAccessToken,
            Guid roleId,
            UpdateTenantRoleBody body,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Patch,
                $"{client.V}/TenantRoles/{roleId}",
                body,
                cancellationToken);

            await KyvoHttpResponse.EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task DeleteAsync(
            string userAccessToken,
            Guid roleId,
            CancellationToken cancellationToken = default)
        {
            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Delete,
                $"{client.V}/TenantRoles/{roleId}",
                null,
                cancellationToken);

            await KyvoHttpResponse.EnsureSuccessAsync(response, cancellationToken);
        }
    }

    private sealed class AuditLogsApi(KyvoProductClient client) : IKyvoAuditLogsApi
    {
        public async Task<PagedResult<AuditLogItemDto>> ListAsync(
            string userAccessToken,
            ListAuditLogsQuery? query = null,
            CancellationToken cancellationToken = default)
        {
            query ??= new ListAuditLogsQuery();
            var qs = new List<string>
            {
                $"page={query.Page}",
                $"pageSize={query.PageSize}"
            };
            if (query.UserId.HasValue) qs.Add($"userId={query.UserId.Value}");
            if (!string.IsNullOrWhiteSpace(query.Action)) qs.Add($"action={Uri.EscapeDataString(query.Action)}");
            if (!string.IsNullOrWhiteSpace(query.ResourceType)) qs.Add($"resourceType={Uri.EscapeDataString(query.ResourceType)}");
            if (query.From.HasValue) qs.Add($"from={Uri.EscapeDataString(query.From.Value.ToString("O"))}");
            if (query.To.HasValue) qs.Add($"to={Uri.EscapeDataString(query.To.Value.ToString("O"))}");

            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Get,
                $"{client.V}/AuditLogs?{string.Join("&", qs)}",
                null,
                cancellationToken);

            return await KyvoHttpResponse.ReadJsonAsync<PagedResult<AuditLogItemDto>>(response, cancellationToken)
                ?? new PagedResult<AuditLogItemDto>([], 0, query.Page, query.PageSize);
        }

        public async Task<PagedResult<AuditLogFilterOptionDto>> ListFilterOptionsAsync(
            string userAccessToken,
            ListAuditLogFilterOptionsQuery query,
            CancellationToken cancellationToken = default)
        {
            var qs = new List<string>
            {
                $"field={Uri.EscapeDataString(query.Field)}",
                $"page={query.Page}",
                $"pageSize={query.PageSize}"
            };

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                qs.Add($"search={Uri.EscapeDataString(query.Search)}");
            }

            var response = await client.SendAsync(
                userAccessToken,
                HttpMethod.Get,
                $"{client.V}/AuditLogs/filter-options?{string.Join("&", qs)}",
                null,
                cancellationToken);

            return await KyvoHttpResponse.ReadJsonAsync<PagedResult<AuditLogFilterOptionDto>>(response, cancellationToken)
                ?? new PagedResult<AuditLogFilterOptionDto>([], 0, query.Page, query.PageSize);
        }
    }
}
