using Kyvo.Application.Queries.Tenants.Dtos;

namespace Kyvo.Application.Queries.Tenants.GetTenantById;

public interface IGetTenantByIdQuery
{
    Task<TenantDto?> ExecuteAsync(GetTenantByIdRequest request, CancellationToken ct = default);
}
