namespace Kyvo.Application.UseCases.Invites.RevokeInvite;

public interface IRevokeInviteUseCase
{
    Task ExecuteAsync(RevokeInviteRequest request, CancellationToken ct = default);
}
