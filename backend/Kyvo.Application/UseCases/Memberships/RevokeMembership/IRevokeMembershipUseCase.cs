namespace Kyvo.Application.UseCases.Memberships.RevokeMembership;

public interface IRevokeMembershipUseCase
{
    Task ExecuteAsync(RevokeMembershipRequest request, CancellationToken ct = default);
}
