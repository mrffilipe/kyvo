using Kyvo.Application.Exceptions;
using Kyvo.Application.Services.Oidc;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Enums;
using Kyvo.Domain.Repositories;

namespace Kyvo.Infrastructure.Services.Oidc;

public sealed class OidcAuthorizeHandler : IOidcAuthorizeHandler
{
    private readonly IAuthSessionRepository _sessions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOidcClientValidator _clientValidator;
    private readonly IOidcAuthorizationService _authorizationService;
    private readonly IOidcClaimsService _claimsService;
    private readonly IPlatformAdminConsoleAccessGuard _platformAdminConsoleAccessGuard;

    public OidcAuthorizeHandler(
        IAuthSessionRepository sessions,
        IUnitOfWork unitOfWork,
        IOidcClientValidator clientValidator,
        IOidcAuthorizationService authorizationService,
        IOidcClaimsService claimsService,
        IPlatformAdminConsoleAccessGuard platformAdminConsoleAccessGuard)
    {
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _clientValidator = clientValidator;
        _authorizationService = authorizationService;
        _claimsService = claimsService;
        _platformAdminConsoleAccessGuard = platformAdminConsoleAccessGuard;
    }

    public async Task<OidcAuthorizeOutcome> HandleAsync(OidcAuthorizeRequest request, OidcCookieAuthenticationState authentication, CancellationToken ct = default)
    {
        var (client, clientError) = await _clientValidator.ValidateClientAsync(request.ClientId, null, ct);
        if (clientError is not null)
        {
            return new OidcAuthorizeRedirectError(request.RedirectUri, request.State, clientError);
        }

        var redirectError = _clientValidator.ValidateRedirectUri(client!, request.RedirectUri);
        if (redirectError is not null)
        {
            return new OidcAuthorizeRedirectError(request.RedirectUri, request.State, redirectError);
        }

        var scopes = _clientValidator.ParseScopes(request.Scope);
        var scopeError = _clientValidator.ValidateScopes(client!, scopes);
        if (scopeError is not null)
        {
            return new OidcAuthorizeRedirectError(request.RedirectUri, request.State, scopeError);
        }

        var pkceError = _clientValidator.ValidatePkceForAuthorize(client!, request.CodeChallenge, request.CodeChallengeMethod);
        if (pkceError is not null)
        {
            return new OidcAuthorizeRedirectError(request.RedirectUri, request.State, pkceError);
        }

        var clientContext = new ApplicationClientValidationContext { Client = client!, Scopes = scopes };
        var requestError = _authorizationService.ValidateAuthorizeRequest(request, clientContext);
        if (requestError is not null)
        {
            return new OidcAuthorizeRedirectError(request.RedirectUri, request.State, requestError);
        }

        var prompt = request.Prompt ?? string.Empty;
        if (!authentication.Succeeded ||
            prompt.Contains("login", StringComparison.OrdinalIgnoreCase) ||
            (request.MaxAge is not null && authentication.IssuedUtc is not null &&
             DateTimeOffset.UtcNow - authentication.IssuedUtc > TimeSpan.FromSeconds(request.MaxAge.Value)))
        {
            if (prompt.Contains("none", StringComparison.OrdinalIgnoreCase))
            {
                return new OidcAuthorizeRedirectError(request.RedirectUri, request.State, new OidcError
                {
                    Error = OidcConstants.Errors.LoginRequired,
                    ErrorDescription = ApplicationErrorMessages.OAuthAuthorization.InteractiveLoginRequired
                });
            }

            return new OidcAuthorizeLoginChallenge();
        }

        if (!authentication.SessionId.HasValue)
        {
            return new OidcAuthorizeRedirectError(request.RedirectUri, request.State, new OidcError
            {
                Error = OidcConstants.Errors.LoginRequired,
                ErrorDescription = ApplicationErrorMessages.OAuthAuthorization.MissingLoginContext
            });
        }

        var session = await _sessions.GetForUpdateAsync(authentication.SessionId.Value, ct);
        if (session is null || session.Status != SessionStatus.Active)
        {
            return new OidcAuthorizeRedirectError(request.RedirectUri, request.State, new OidcError
            {
                Error = OidcConstants.Errors.LoginRequired,
                ErrorDescription = ApplicationErrorMessages.OAuthAuthorization.SessionNoLongerActive
            });
        }

        if (!session.ClientId.HasValue)
        {
            session.BindOAuthClient(client!.Id);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        var accessError = await _platformAdminConsoleAccessGuard.TryValidateAccessAsync(session.UserId, client!.ClientId, ct);
        if (accessError is not null)
        {
            return new OidcAuthorizeRedirectError(request.RedirectUri, request.State, accessError);
        }

        var claims = await _claimsService.TryBuildClaimsAsync(session.Id, scopes, ct);
        if (claims is null)
        {
            return new OidcAuthorizeRedirectError(request.RedirectUri, request.State, new OidcError
            {
                Error = OidcConstants.Errors.LoginRequired,
                ErrorDescription = ApplicationErrorMessages.OAuthAuthorization.UnableToBuildClaims
            });
        }

        var (code, codeError) = await _authorizationService.CreateAuthorizationCodeAsync(
            request,
            session.Id,
            client!.Id,
            scopes,
            ct);
        if (codeError is not null)
        {
            return new OidcAuthorizeRedirectError(request.RedirectUri, request.State, codeError);
        }

        return new OidcAuthorizeSuccess(request.RedirectUri, code!, request.State);
    }
}
