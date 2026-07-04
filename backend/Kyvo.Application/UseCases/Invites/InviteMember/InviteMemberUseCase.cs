using Kyvo.Application.Common;
using Kyvo.Application.Interfaces;
using Kyvo.Application.Policies;
using Kyvo.Application.Ports.Email;
using Kyvo.Application.Services.Security;
using Kyvo.Application.Services.TenantRoles;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Invites.InviteMember;

public sealed class InviteMemberUseCase : IInviteMemberUseCase
{
    private readonly ITenantRepository _tenants;
    private readonly ITenantInviteRepository _invites;
    private readonly IInviteTokenHasher _hasher;
    private readonly IInvitePolicy _policy;
    private readonly ITenantRoleResolver _roleResolver;
    private readonly IEmailService _emailService;
    private readonly IInviteTokenProtector _inviteTokenProtector;
    private readonly ITenantAuthorizationPolicy _authorizationPolicy;
    private readonly IUnitOfWork _unitOfWork;

    public InviteMemberUseCase(
        ITenantRepository tenants,
        ITenantInviteRepository invites,
        IInviteTokenHasher hasher,
        IInvitePolicy policy,
        ITenantRoleResolver roleResolver,
        IEmailService emailService,
        IInviteTokenProtector inviteTokenProtector,
        ITenantAuthorizationPolicy authorizationPolicy,
        IUnitOfWork unitOfWork)
    {
        _tenants = tenants;
        _invites = invites;
        _hasher = hasher;
        _policy = policy;
        _roleResolver = roleResolver;
        _emailService = emailService;
        _inviteTokenProtector = inviteTokenProtector;
        _authorizationPolicy = authorizationPolicy;
        _unitOfWork = unitOfWork;
    }

    public async Task<InviteMemberResult> ExecuteAsync(InviteMemberRequest request, CancellationToken ct = default)
    {
        await _authorizationPolicy.EnsureTenantAdministrativeAccessAsync(
            request.TenantId,
            request.ActorUserId,
            request.ActorPlatformRoles,
            ct);

        var tenant = await _tenants.GetForUpdateAsync(request.TenantId, ct)
            ?? throw new DomainNotFoundException(DomainErrorMessages.Tenant.TENANT_NOT_FOUND);

        var roles = await _roleResolver.ResolveActiveRolesAsync(
            request.TenantId,
            request.Roles,
            ct);

        var rawToken = GenerateToken();
        var acceptPath = InviteAcceptPath.Build(rawToken);

        await _emailService.SendInviteAsync(
            request.Email.Trim(),
            tenant.Name,
            rawToken,
            acceptPath,
            ct);

        var invite = new TenantInvite(
            request.TenantId,
            request.Email,
            roles,
            _hasher.Hash(rawToken),
            _inviteTokenProtector.Protect(rawToken),
            DateTime.UtcNow.AddHours(_policy.ExpirationHours),
            request.InvitedByUserId);

        await _invites.AddAsync(invite, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new InviteMemberResult
        {
            Id = invite.Id,
            AcceptPath = acceptPath
        };
    }

    private static string GenerateToken()
    {
        var bytes = new byte[64];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
