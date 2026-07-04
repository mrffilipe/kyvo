namespace Kyvo.Application.UseCases.TenantRoles.CreateTenantRole;

public interface ICreateTenantRoleUseCase
{
    Task<Guid> ExecuteAsync(CreateTenantRoleRequest request, CancellationToken ct = default);
}
