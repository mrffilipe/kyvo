namespace Kyvo.Application.Services.AccountBranding;

/// <summary>
/// Resolves account branding once per request (and per returnUrl/clientId key) to avoid concurrent EF access on the same DbContext.
/// </summary>
public interface IAccountBrandingStateProvider
{
    Task<AccountBrandingViewModel> GetBrandingAsync(string? returnUrl, string? clientIdQuery, CancellationToken cancellationToken = default);
}
