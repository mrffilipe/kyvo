using Kyvo.Application.Common;
using Kyvo.Application.Exceptions;
using Kyvo.Application.Ports.Oidc;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Applications.CreateApplicationClient;

public sealed class CreateApplicationClientUseCase : ICreateApplicationClientUseCase
{
    private readonly IOAuthClientManager _oauthClients;
    private readonly IUnitOfWork _unitOfWork;

    public CreateApplicationClientUseCase(IOAuthClientManager oauthClients, IUnitOfWork unitOfWork)
    {
        _oauthClients = oauthClients;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> ExecuteAsync(CreateApplicationClientRequest request, CancellationToken ct = default)
    {
        if (!request.ActorPlatformRoles.Any(role => PlatformRoleDefaults.AdministrativeKeys.Contains(role)))
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.USER_HAS_NO_TENANT_ACCESS);
        }

        var id = await _oauthClients.CreateAsync(new CreateOAuthClientRequest
        {
            ApplicationId = request.ApplicationId,
            ClientId = request.ClientId,
            ClientType = request.ClientType,
            RedirectUris = OAuthClientFieldValidator.ParseAndValidateRedirectUris(request.RedirectUris),
            PostLogoutRedirectUris = OAuthClientFieldValidator.ParseAndValidatePostLogoutRedirectUris(request.PostLogoutRedirectUris),
            AllowedScopes = OAuthClientFieldValidator.ParseAndValidateAllowedScopes(request.AllowedScopes, request.AllowedScopesList),
            AccessTokenTtlSeconds = request.AccessTokenTtlSeconds,
            ClientSecret = request.ClientSecret,
            IsSystem = false,
            RequireExplicitConsent = true
        }, ct);

        await _unitOfWork.SaveChangesAsync(ct);
        return id;
    }
}
