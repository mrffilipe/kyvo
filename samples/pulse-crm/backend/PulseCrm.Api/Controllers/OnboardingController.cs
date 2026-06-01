using Kyvo.AspNetCore;
using Kyvo.Client;
using Kyvo.Client.Exceptions;
using Kyvo.Client.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PulseCrm.Api.Data;
using PulseCrm.Api.Helpers;
using PulseCrm.Api.Models;

namespace PulseCrm.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/onboarding")]
public sealed class OnboardingController : ControllerBase
{
    private readonly IKyvoUserContext _user;
    private readonly PulseCrmDbContext _db;
    private readonly IKyvoProductClient _kyvo;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OnboardingController(
        IKyvoUserContext user,
        PulseCrmDbContext db,
        IKyvoProductClient kyvo,
        IHttpContextAccessor httpContextAccessor)
    {
        _user = user;
        _db = db;
        _kyvo = kyvo;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("complete")]
    [ProducesResponseType(typeof(OnboardingCompleteResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OnboardingCompleteResponse>> Complete(
        [FromBody] CompleteOnboardingBody body,
        CancellationToken cancellationToken)
    {
        if (_user.UserId is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(body.CompanyName) || string.IsNullOrWhiteSpace(body.PlanCode))
        {
            return BadRequest(new { message = "companyName and planCode are required." });
        }

        var existing = await _db.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == _user.UserId, cancellationToken);

        if (existing is not null)
        {
            return Conflict(new { message = "User already completed onboarding.", subscription = existing });
        }

        var accessToken = _httpContextAccessor.GetUserAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Unauthorized(new { message = "Bearer access token required." });
        }

        var tenantKey = SlugHelper.ToTenantKey(body.CompanyName);
        var externalCustomerId = body.PaymentReference ?? $"pay_mock_{Guid.NewGuid():N}"[..24];

        SubscribeTenantResult kyvoSubscribe;
        try
        {
            kyvoSubscribe = await _kyvo.Auth.SubscribeAsync(
                accessToken,
                new SubscribeTenantRequest(
                    body.CompanyName.Trim(),
                    tenantKey,
                    body.PlanCode.Trim().ToLowerInvariant(),
                    externalCustomerId),
                cancellationToken);
        }
        catch (KyvoApiException ex)
        {
            return StatusCode((int)ex.StatusCode, new { message = ex.Detail ?? ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new { message = ex.Message });
        }

        var idpContext = kyvoSubscribe.Context;
        if (idpContext.TenantId is null || idpContext.MembershipId is null)
        {
            return StatusCode(502, new { message = "Kyvo subscribe did not return tenant context. Refresh token and retry." });
        }

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            UserId = _user.UserId.Value,
            TenantId = idpContext.TenantId.Value,
            MembershipId = idpContext.MembershipId.Value,
            CompanyName = body.CompanyName.Trim(),
            TenantKey = tenantKey,
            PlanCode = body.PlanCode.Trim().ToLowerInvariant(),
            ExternalCustomerId = externalCustomerId,
            PaidAt = DateTime.UtcNow
        };

        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync(cancellationToken);

        var hasFreshTokens = kyvoSubscribe.Tokens is not null;

        return Ok(new OnboardingCompleteResponse(
            subscription,
            idpContext,
            kyvoSubscribe.Tokens,
            RequiresTokenRefresh: !hasFreshTokens,
            Message: hasFreshTokens
                ? "Onboarding complete. Session tokens include tid/mid."
                : "Onboarding complete. Refresh OIDC tokens to receive tid/mid claims."));
    }

    public sealed record CompleteOnboardingBody(
        string CompanyName,
        string PlanCode,
        string? PaymentReference);
}
