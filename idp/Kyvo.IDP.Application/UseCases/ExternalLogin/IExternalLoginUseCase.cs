using System.Security.Claims;
using Kyvo.IDP.Application.Services.AccountLinking;

namespace Kyvo.IDP.Application.UseCases.ExternalLogin;

public interface IExternalLoginUseCase
{
    Task<AccountLinkResult> ExecuteAsync(ClaimsPrincipal externalPrincipal, string provider, CancellationToken ct = default);
}
