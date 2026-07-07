using System.Security.Claims;
using Kyvo.Application.Configurations;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Application.UseCases.Auth.ExternalLogin;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Repositories;
using Kyvo.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Kyvo.API.Services;

public interface IAccountSignInService
{
    Task<AuthSession> SignInAsync(HttpContext httpContext, ExternalLoginResult login, CancellationToken ct = default);
}

public sealed class AccountSignInService : IAccountSignInService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthSessionRepository _sessions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtOptions _jwtOptions;

    public AccountSignInService(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuthSessionRepository sessions,
        IUnitOfWork unitOfWork,
        IOptions<JwtOptions> jwtOptions)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthSession> SignInAsync(HttpContext httpContext, ExternalLoginResult login, CancellationToken ct = default)
    {
        var activeMembership = login.TenantMemberships.FirstOrDefault();

        var session = new AuthSession(
            login.UserId,
            activeMembership?.TenantId,
            activeMembership?.MembershipId,
            DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            httpContext.Request.Headers.UserAgent.ToString(),
            httpContext.Connection.RemoteIpAddress?.ToString());

        await _sessions.AddAsync(session, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var user = await _userManager.FindByIdAsync(login.UserId.ToString("D"))
            ?? throw new InvalidOperationException($"User '{login.UserId}' not found right after sign-in.");

        await _signInManager.SignInWithClaimsAsync(
            user,
            isPersistent: true,
            additionalClaims: [new Claim("sid", session.Id.ToString("D"))]);

        return session;
    }
}
