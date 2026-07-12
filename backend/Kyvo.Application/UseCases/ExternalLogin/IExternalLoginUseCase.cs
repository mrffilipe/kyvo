using System.Security.Claims;
using Kyvo.Application.Services.AccountLinking;

namespace Kyvo.Application.UseCases.ExternalLogin;

public interface IExternalLoginUseCase
{
    Task<AccountLinkResult> ExecuteAsync(ClaimsPrincipal externalPrincipal, string provider, CancellationToken ct = default);
}
