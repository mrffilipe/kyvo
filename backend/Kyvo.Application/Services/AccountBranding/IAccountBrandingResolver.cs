namespace Kyvo.Application.Services.AccountBranding;

public interface IAccountBrandingResolver
{
    Task<AccountBrandingViewModel> ResolveAsync(string? returnUrl, string? clientIdQuery, CancellationToken ct = default);
}
