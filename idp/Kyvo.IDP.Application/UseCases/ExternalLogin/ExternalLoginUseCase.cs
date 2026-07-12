using System.Security.Claims;
using Kyvo.IDP.Application.Services.AccountLinking;
using Kyvo.IDP.Application.Services.Claims;

namespace Kyvo.IDP.Application.UseCases.ExternalLogin;

public sealed class ExternalLoginUseCase : IExternalLoginUseCase
{
    private readonly IClaimsMappingService _claimsMapping;
    private readonly IAccountLinkingService _accountLinking;

    public ExternalLoginUseCase(IClaimsMappingService claimsMapping, IAccountLinkingService accountLinking)
    {
        _claimsMapping = claimsMapping;
        _accountLinking = accountLinking;
    }

    public Task<AccountLinkResult> ExecuteAsync(ClaimsPrincipal externalPrincipal, string provider, CancellationToken ct = default)
    {
        var mapped = _claimsMapping.Map(externalPrincipal, provider);
        return _accountLinking.LinkOrCreateAsync(mapped, ct);
    }
}
