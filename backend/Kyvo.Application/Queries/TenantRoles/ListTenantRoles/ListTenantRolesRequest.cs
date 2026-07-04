using Kyvo.Application.Common;

namespace Kyvo.Application.Queries.TenantRoles.ListTenantRoles;

public sealed record ListTenantRolesRequest : PagedRequest
{
    public required Guid TenantId { get; init; }
    public required bool IncludeInactive { get; init; }
}
