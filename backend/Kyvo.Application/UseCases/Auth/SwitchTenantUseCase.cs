using Kyvo.Application.Exceptions;
using Kyvo.Application.Ports.Oidc;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Application.Services.UserScope;
using Kyvo.Application.Shared;
using Kyvo.Application.UseCases.Auth.SwitchTenant;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Auth;

public sealed class SwitchTenantUseCase : ISwitchTenantUseCase
{
    private readonly IUserRepository _users;
    private readonly ITenantMembershipRepository _memberships;
    private readonly IAuthSessionRepository _sessions;
    private readonly IUserPlatformRoleRepository _userPlatformRoles;
    private readonly ITenantAccessTokenIssuer _tenantTokenIssuer;
    private readonly IUserScope _userScope;
    private readonly IUnitOfWork _unitOfWork;

    public SwitchTenantUseCase(
        IUserRepository users,
        ITenantMembershipRepository memberships,
        IAuthSessionRepository sessions,
        IUserPlatformRoleRepository userPlatformRoles,
        IUserScope userScope,
        IUnitOfWork unitOfWork,
        ITenantAccessTokenIssuer tenantTokenIssuer)
    {
        _users = users;
        _memberships = memberships;
        _sessions = sessions;
        _userPlatformRoles = userPlatformRoles;
        _userScope = userScope;
        _unitOfWork = unitOfWork;
        _tenantTokenIssuer = tenantTokenIssuer;
    }

    public async Task<TenantContextResult> ExecuteAsync(SwitchTenantRequest request, CancellationToken ct = default)
    {
        if (!_userScope.IsAuthenticated || _userScope.UserId == Guid.Empty || !_userScope.SessionId.HasValue)
        {
            throw new UnauthorizedApplicationException(ApplicationErrorMessages.Auth.AUTHENTICATED_SESSION_REQUIRED);
        }

        var membership = await _memberships.GetByUserIdAndTenantIdWithRolesAsync(
            _userScope.UserId,
            request.TenantId,
            ct);

        if (membership is null || !membership.IsActive)
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.USER_HAS_NO_TENANT_ACCESS);
        }

        var session = await _sessions.GetForUpdateAsync(_userScope.SessionId.Value, ct)
            ?? throw new UnauthorizedApplicationException(ApplicationErrorMessages.Auth.SESSION_NOT_FOUND);

        session.SwitchTenant(membership.TenantId, membership.Id);
        await _unitOfWork.SaveChangesAsync(ct);

        var user = await _users.GetForUpdateAsync(_userScope.UserId, ct)
            ?? throw new UnauthorizedApplicationException(ApplicationErrorMessages.Auth.USER_NOT_FOUND);

        var memberships = await _memberships.ListByUserIdWithTenantAndRolesAsync(user.Id, ct);
        var platformRoles = await ResolvePlatformRolesAsync(user.Id, ct);
        var result = TenantContextBuilder.Build(user, session, memberships, platformRoles);
        var accessToken = _tenantTokenIssuer.IssueToken(session, platformRoles, result.TenantRoles);
        return result with
        {
            AccessToken = accessToken,
            ExpiresIn = 900,
            TokenType = "Bearer"
        };
    }

    private async Task<IReadOnlyList<string>> ResolvePlatformRolesAsync(Guid userId, CancellationToken ct)
    {
        var assignments = await _userPlatformRoles.ListByUserIdAsync(userId, ct);
        return assignments.Select(x => x.Role.Key).ToList();
    }
}
