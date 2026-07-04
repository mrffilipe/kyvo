namespace Kyvo.Application.UseCases.Memberships.UpdateMembershipRoles;

public interface IUpdateMembershipRolesUseCase
{
    Task ExecuteAsync(UpdateMembershipRolesRequest request, CancellationToken ct = default);
}
