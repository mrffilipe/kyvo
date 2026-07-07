using Kyvo.Application.Exceptions;
using Kyvo.Application.Ports.Oidc;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Application.Services.UserScope;
using Kyvo.Application.Shared;
using Kyvo.Application.UseCases.Auth.SubscribeTenant;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Auth;

public sealed class SubscribeTenantUseCase : ISubscribeTenantUseCase
{
    private readonly ITenantProvisioner _tenantProvisioner;
    private readonly IUserRepository _users;
    private readonly ITenantMembershipRepository _memberships;
    private readonly IAuthSessionRepository _sessions;
    private readonly IOAuthClientManager _oauthClients;
    private readonly IApplicationRepository _applications;
    private readonly IApplicationTenantRepository _applicationTenants;
    private readonly IUserPlatformRoleRepository _userPlatformRoles;
    private readonly IUserScope _userScope;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantAccessTokenIssuer _tenantTokenIssuer;

    public SubscribeTenantUseCase(
        ITenantProvisioner tenantProvisioner,
        IUserRepository users,
        ITenantMembershipRepository memberships,
        IAuthSessionRepository sessions,
        IOAuthClientManager oauthClients,
        IApplicationRepository applications,
        IApplicationTenantRepository applicationTenants,
        IUserPlatformRoleRepository userPlatformRoles,
        IUserScope userScope,
        IUnitOfWork unitOfWork,
        ITenantAccessTokenIssuer tenantTokenIssuer)
    {
        _tenantProvisioner = tenantProvisioner;
        _users = users;
        _memberships = memberships;
        _sessions = sessions;
        _oauthClients = oauthClients;
        _applications = applications;
        _applicationTenants = applicationTenants;
        _userPlatformRoles = userPlatformRoles;
        _userScope = userScope;
        _unitOfWork = unitOfWork;
        _tenantTokenIssuer = tenantTokenIssuer;
    }

    public async Task<TenantContextResult> ExecuteAsync(SubscribeTenantRequest request, CancellationToken ct = default)
    {
        if (!_userScope.IsAuthenticated || _userScope.UserId == Guid.Empty || !_userScope.SessionId.HasValue)
        {
            throw new UnauthorizedApplicationException(ApplicationErrorMessages.Auth.AUTHENTICATED_SESSION_REQUIRED);
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

        var application = await _applications.GetByIdAsync(client.ApplicationId, ct)
            ?? throw new DomainNotFoundException(ApplicationErrorMessages.Application.NOT_FOUND);

        var provisionResult = await _tenantProvisioner.ProvisionAsync(
            new TenantProvisionRequest
            {
                TenantName = request.TenantName,
                TenantKey = request.TenantKey,
                OwnerUserId = _userScope.UserId
            },
            ct);

        var applicationTenant = new ApplicationTenant(
            application.Id,
            provisionResult.TenantId,
            request.ExternalCustomerId,
            request.PlanCode);

        if (await _applicationTenants.MappingAlreadyExistsAsync(application.Id, provisionResult.TenantId, ct))
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.ApplicationTenant.MAPPING_ALREADY_EXISTS);
        }

        await _applicationTenants.AddAsync(applicationTenant, ct);

        session.SwitchTenant(provisionResult.TenantId, provisionResult.MembershipId);
        await _unitOfWork.SaveChangesAsync(ct);

        var user = await _users.GetForUpdateAsync(_userScope.UserId, ct)
            ?? throw new DomainNotFoundException(DomainErrorMessages.User.USER_NOT_FOUND);

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
