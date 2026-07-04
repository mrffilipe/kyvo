namespace Kyvo.Application.UseCases.Invites.AcceptInvite;

public interface IAcceptInviteUseCase
{
    Task<Guid> ExecuteAsync(AcceptInviteRequest request, CancellationToken ct = default);
}
