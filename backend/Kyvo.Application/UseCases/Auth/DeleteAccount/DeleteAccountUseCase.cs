using Kyvo.Application.Exceptions;
using Kyvo.Application.Policies;
using Kyvo.Application.Ports.Oidc;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Application.Services.UserScope;
using Kyvo.Application.UseCases.Auth.DeleteAccount;
using Kyvo.Application.UseCases.Auth.DeleteTenant;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Auth;

public sealed class DeleteAccountUseCase : IDeleteAccountUseCase
{
    private readonly ITenantMembershipRepository _memberships;
    private readonly IAuthSessionRepository _sessions;
    private readonly IOAuthClientManager _oauthClients;
    private readonly IUserRepository _users;
    private readonly ITenantAccountEligibilityPolicy _accountEligibility;
    private readonly IDeleteTenantUseCase _deleteTenantUseCase;
    private readonly IUserScope _userScope;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAccountUseCase(
        ITenantMembershipRepository memberships,
        IAuthSessionRepository sessions,
        IOAuthClientManager oauthClients,
        IUserRepository users,
        ITenantAccountEligibilityPolicy accountEligibility,
        IDeleteTenantUseCase deleteTenantUseCase,
        IUserScope userScope,
        IUnitOfWork unitOfWork)
    {
        _memberships = memberships;
        _sessions = sessions;
        _oauthClients = oauthClients;
        _users = users;
        _accountEligibility = accountEligibility;
        _deleteTenantUseCase = deleteTenantUseCase;
        _userScope = userScope;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        if (!_userScope.IsAuthenticated || _userScope.UserId == Guid.Empty || !_userScope.SessionId.HasValue)
        {
            throw new UnauthorizedApplicationException(ApplicationErrorMessages.Auth.AUTHENTICATED_SESSION_REQUIRED);
        }

        if (!_userScope.TenantId.HasValue)
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.ACTIVE_TENANT_CONTEXT_REQUIRED);
        }

        if (string.IsNullOrWhiteSpace(_userScope.OAuthClientId))
        {
            throw new InvalidClientException(ApplicationErrorMessages.Auth.SESSION_HAS_NO_OAUTH_CLIENT);
        }

        var session = await _sessions.GetForUpdateAsync(_userScope.SessionId.Value, ct)
            ?? throw new UnauthorizedApplicationException(ApplicationErrorMessages.Auth.SESSION_NOT_FOUND);

        if (session.UserId != _userScope.UserId)
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.CANNOT_REVOKE_ANOTHER_USER_SESSION);
        }

        var client = await _oauthClients.GetByClientIdAsync(_userScope.OAuthClientId, ct)
            ?? throw new InvalidClientException(ApplicationErrorMessages.OAuthClient.CLIENT_ID_INVALID);

        var tenantId = _userScope.TenantId.Value;
        await _accountEligibility.EnsureCanDeleteAccountAsync(client.ApplicationId, tenantId, ct);

        var membership = await _memberships.GetByUserIdAndTenantIdWithRolesAsync(
            _userScope.UserId,
            tenantId,
            ct);

        if (membership is null || !membership.IsActive)
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.USER_HAS_NO_TENANT_ACCESS);
        }

        var isOwner = membership.Roles.Any(role =>
            role.Role.Key.Value.Equals(TenantRoleDefaults.OWNER, StringComparison.OrdinalIgnoreCase));

        if (isOwner)
        {
            await _deleteTenantUseCase.ExecuteAsync(tenantId, ct);
        }
        else
        {
            membership.Revoke();
            await RevokeUserSessionsForTenantAsync(_userScope.UserId, tenantId, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        await DeactivateUserIfNoActiveMembershipsAsync(_userScope.UserId, ct);
    }

    private async Task RevokeUserSessionsForTenantAsync(Guid userId, Guid tenantId, CancellationToken ct)
    {
        var sessions = await _sessions.ListActiveByUserIdAndTenantIdForUpdateAsync(userId, tenantId, ct);
        foreach (var activeSession in sessions)
        {
            activeSession.Revoke();
        }
    }

    private async Task DeactivateUserIfNoActiveMembershipsAsync(Guid userId, CancellationToken ct)
    {
        var hasActiveMembership = await _memberships.HasActiveMembershipAsync(userId, ct);

        if (hasActiveMembership)
        {
            return;
        }

        var user = await _users.GetForUpdateAsync(userId, ct)
            ?? throw new DomainNotFoundException(DomainErrorMessages.User.USER_NOT_FOUND);

        user.Deactivate();

        var sessions = await _sessions.ListActiveByUserIdAsync(userId, ct);
        foreach (var activeSession in sessions)
        {
            var tracked = await _sessions.GetForUpdateAsync(activeSession.Id, ct);
            tracked?.Revoke();
        }

        await _users.SyncFromDomainAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
