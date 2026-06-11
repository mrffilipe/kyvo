using Kyvo.Application.Common;

namespace Kyvo.Application.Services.Membership;

public interface IMembershipService
{
    Task<Guid> CreateMembershipAsync(CreateMembershipRequest request, CancellationToken cancellationToken = default);
    Task UpdateRolesAsync(UpdateMembershipRolesRequest request, CancellationToken cancellationToken = default);
    Task RevokeMembershipAsync(RevokeMembershipRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<MembershipDto>> ListByTenantAsync(ListMembershipsByTenantRequest request, CancellationToken cancellationToken = default);
}
