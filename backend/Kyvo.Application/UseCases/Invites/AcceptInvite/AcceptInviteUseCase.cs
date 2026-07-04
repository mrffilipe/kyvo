using Kyvo.Application.Services.Security;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Invites.AcceptInvite;

public sealed class AcceptInviteUseCase : IAcceptInviteUseCase
{
    private readonly ITenantInviteRepository _invites;
    private readonly IUserRepository _users;
    private readonly ITenantMembershipRepository _memberships;
    private readonly IInviteTokenHasher _hasher;
    private readonly IUnitOfWork _unitOfWork;

    public AcceptInviteUseCase(
        ITenantInviteRepository invites,
        IUserRepository users,
        ITenantMembershipRepository memberships,
        IInviteTokenHasher hasher,
        IUnitOfWork unitOfWork)
    {
        _invites = invites;
        _users = users;
        _memberships = memberships;
        _hasher = hasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> ExecuteAsync(AcceptInviteRequest request, CancellationToken ct = default)
    {
        var tokenHash = _hasher.Hash(request.InviteToken);
        var invite = await _invites.GetByTokenHashWithRolesAsync(tokenHash, ct)
            ?? throw new DomainNotFoundException(DomainErrorMessages.TenantInvite.INVITE_NOT_FOUND);

        if (invite.IsConsumed())
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantInvite.ALREADY_CONSUMED);
        }

        if (invite.IsExpired())
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantInvite.EXPIRED);
        }

        if (invite.IsRevoked())
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantInvite.REVOKED);
        }

        var user = await _users.GetForUpdateAsync(request.ActorUserId, ct)
            ?? throw new DomainNotFoundException(DomainErrorMessages.User.USER_NOT_FOUND);

        if (!string.Equals(user.Email, invite.Email.Value, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.TenantInvite.EMAIL_MISMATCH);
        }

        var membership = await _memberships.GetByUserIdAndTenantIdWithRolesAsync(
            user.Id,
            invite.TenantId,
            ct);

        if (membership is null || !membership.IsActive)
        {
            membership = new TenantMembership(
                invite.TenantId,
                user.Id,
                invite.Roles.Select(x => x.Role));
            await _memberships.AddAsync(membership, ct);
        }
        else
        {
            membership.MergeRoles(invite.Roles.Select(x => x.Role));
        }

        invite.Consume();
        await _unitOfWork.SaveChangesAsync(ct);
        return membership.Id;
    }
}
