namespace Kyvo.Application.UseCases.TenantRoles.UpdateTenantRole;

public interface IUpdateTenantRoleUseCase
{
    Task ExecuteAsync(UpdateTenantRoleRequest request, CancellationToken ct = default);
}
