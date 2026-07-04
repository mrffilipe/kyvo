using Kyvo.Application.UseCases.Memberships.CreateMembership;

namespace Kyvo.Application.UseCases.Memberships.CreateMembership;

public interface ICreateMembershipUseCase
{
    Task<Guid> ExecuteAsync(CreateMembershipRequest request, CancellationToken ct = default);
}
