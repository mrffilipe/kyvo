namespace Kyvo.Application.UseCases.Invites.InviteMember;

public interface IInviteMemberUseCase
{
    Task<InviteMemberResult> ExecuteAsync(InviteMemberRequest request, CancellationToken ct = default);
}
