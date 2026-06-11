using Kyvo.Application.Common;

namespace Kyvo.Application.Services.TenantRoles;

public interface ITenantRoleService
{
    Task<Guid> CreateTenantRoleAsync(CreateTenantRoleRequest request, CancellationToken cancellationToken = default);
    Task UpdateTenantRoleAsync(UpdateTenantRoleRequest request, CancellationToken cancellationToken = default);
    Task DeleteTenantRoleAsync(DeleteTenantRoleRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<TenantRoleDto>> ListTenantRolesAsync(ListTenantRolesRequest request, CancellationToken cancellationToken = default);
}
