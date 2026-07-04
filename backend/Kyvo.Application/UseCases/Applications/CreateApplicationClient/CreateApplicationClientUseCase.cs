using Kyvo.Application.Common;
using Kyvo.Application.Exceptions;
using Kyvo.Application.Ports.Oidc;
using Kyvo.Application.Services.UnitOfWork;
using Kyvo.Domain.Constants;
using Kyvo.Domain.Entities;
using Kyvo.Domain.Repositories;

namespace Kyvo.Application.UseCases.Applications.CreateApplicationClient;

public sealed class CreateApplicationClientUseCase : ICreateApplicationClientUseCase
{
    private readonly IApplicationClientRepository _clients;
    private readonly IOpenIddictApplicationSyncService _openIddictSync;
    private readonly IUnitOfWork _unitOfWork;

    public CreateApplicationClientUseCase(
        IApplicationClientRepository clients,
        IOpenIddictApplicationSyncService openIddictSync,
        IUnitOfWork unitOfWork)
    {
        _clients = clients;
        _openIddictSync = openIddictSync;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> ExecuteAsync(CreateApplicationClientRequest request, CancellationToken ct = default)
    {
        if (!request.ActorPlatformRoles.Any(role => PlatformRoleDefaults.AdministrativeKeys.Contains(role)))
        {
            throw new ForbiddenApplicationException(ApplicationErrorMessages.Auth.USER_HAS_NO_TENANT_ACCESS);
        }

        var client = new ApplicationClient(
            request.ApplicationId,
            request.ClientId,
            request.ClientType,
            ApplicationClientListFields.ParseAndValidateRedirectUris(request.RedirectUris),
            ApplicationClientListFields.ParseAndValidateAllowedScopes(request.AllowedScopes, request.AllowedScopesList),
            request.AccessTokenTtlSeconds,
            ApplicationClientListFields.ParseAndValidatePostLogoutRedirectUris(request.PostLogoutRedirectUris));

        await _clients.AddAsync(client, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await _openIddictSync.SyncAsync(client, request.ClientSecret, ct);
        return client.Id;
    }
}
