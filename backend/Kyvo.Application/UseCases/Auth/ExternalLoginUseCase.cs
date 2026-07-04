using Kyvo.Application.Ports.Identity;
using Kyvo.Application.UseCases.Auth.ExternalLogin;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;
using Kyvo.Domain.ValueObjects;

namespace Kyvo.Application.UseCases.Auth;

public sealed class ExternalLoginUseCase : IExternalLoginUseCase
{
    private readonly IUserAccountService _userAccounts;
    private readonly ITenantMembershipRepository _memberships;
    private readonly IUserPlatformRoleRepository _userPlatformRoles;

    public ExternalLoginUseCase(
        IUserAccountService userAccounts,
        ITenantMembershipRepository memberships,
        IUserPlatformRoleRepository userPlatformRoles)
    {
        _userAccounts = userAccounts;
        _memberships = memberships;
        _userPlatformRoles = userPlatformRoles;
    }

    public async Task<ExternalLoginResult> ExecuteAsync(ExternalLoginRequest request, CancellationToken ct = default)
    {
        var user = await _userAccounts.FindByLoginAsync(request.ProviderAlias, request.ProviderUserId, ct);

        if (user is null)
        {
            var email = new EmailAddress(request.Email);
            user = await _userAccounts.FindByEmailAsync(email.Value, ct);

            if (user is null)
            {
                user = new User(email, request.DisplayName ?? email.Value.Split('@')[0]);
                var createResult = await _userAccounts.CreateAsync(user, ct);
                if (!createResult.Succeeded)
                {
                    throw new DomainBusinessRuleException(string.Join(" ", createResult.Errors));
                }
            }

            var addLoginResult = await _userAccounts.AddLoginAsync(
                user.Id,
                request.ProviderAlias,
                request.ProviderUserId,
                ct);
            if (!addLoginResult.Succeeded)
            {
                throw new DomainBusinessRuleException(string.Join(" ", addLoginResult.Errors));
            }
        }

        if (!user.IsActive)
        {
            throw new DomainBusinessRuleException(DomainErrorMessages.User.USER_NOT_FOUND);
        }

        var memberships = await _memberships.ListByUserIdWithTenantAndRolesAsync(user.Id, ct);
        var platformRoleAssignments = await _userPlatformRoles.ListByUserIdAsync(user.Id, ct);
        var platformRoles = platformRoleAssignments.Select(x => x.Role.Key).ToList();

        return new ExternalLoginResult
        {
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            PlatformRoles = platformRoles,
            TenantMemberships = memberships
                .Select(m => new ExternalLoginTenantMembership
                {
                    TenantId = m.TenantId,
                    MembershipId = m.Id,
                    Roles = m.Roles.Select(r => r.Role.Key.Value).ToList()
                })
                .ToList()
        };
    }
}
