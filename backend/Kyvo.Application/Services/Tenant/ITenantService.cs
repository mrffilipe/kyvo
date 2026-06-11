using Kyvo.Application.Common;

namespace Kyvo.Application.Services.Tenant;

public interface ITenantService
{
    Task<Guid> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);
    Task UpdateTenantAsync(UpdateTenantRequest request, CancellationToken cancellationToken = default);
    Task<TenantDto?> GetTenantByIdAsync(GetTenantByIdRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<TenantDto>> ListByUserAsync(ListTenantsByUserRequest request, CancellationToken cancellationToken = default);
    Task<AvailabilityDto> IsKeyAvailableAsync(string key, CancellationToken cancellationToken = default);
    Task<InviteMemberResult> InviteMemberAsync(InviteMemberRequest request, CancellationToken cancellationToken = default);
    Task<Guid> AcceptInviteAsync(AcceptInviteRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<TenantInviteDto>> ListInvitesByTenantAsync(ListInvitesByTenantRequest request, CancellationToken cancellationToken = default);
    Task RevokeInviteAsync(RevokeInviteRequest request, CancellationToken cancellationToken = default);
}
