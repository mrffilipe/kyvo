using Kyvo.Application.Policies;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Invites.RevokeInvite;

public sealed class RevokeInviteUseCase : IRevokeInviteUseCase
{
    private readonly ITenantInviteRepository _invites;
    private readonly ITenantAuthorizationPolicy _authorizationPolicy;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeInviteUseCase(
        ITenantInviteRepository invites,
        ITenantAuthorizationPolicy authorizationPolicy,
        IUnitOfWork unitOfWork)
    {
        _invites = invites;
        _authorizationPolicy = authorizationPolicy;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(RevokeInviteRequest request, CancellationToken ct = default)
    {
        var invite = await _invites.GetForUpdateAsync(request.InviteId, ct)
            ?? throw new DomainNotFoundException(DomainErrorMessages.TenantInvite.INVITE_NOT_FOUND);

        await _authorizationPolicy.EnsureTenantAdministrativeAccessAsync(
            invite.TenantId,
            request.ActorUserId,
            request.ActorPlatformRoles,
            ct);

        invite.Revoke();
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
