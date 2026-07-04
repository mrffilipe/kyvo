namespace Kyvo.Application.UseCases.TenantRoles.DeleteTenantRole;

public interface IDeleteTenantRoleUseCase
{
    Task ExecuteAsync(DeleteTenantRoleRequest request, CancellationToken ct = default);
}
