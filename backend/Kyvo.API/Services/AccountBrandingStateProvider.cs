using Kyvo.Application.Services.AccountBranding;

namespace Kyvo.API.Services;

public sealed class AccountBrandingStateProvider : IAccountBrandingStateProvider
{
    private readonly IAccountBrandingResolver _resolver;
    private string? _cacheKey;
    private Task<AccountBrandingViewModel>? _brandingTask;

    public AccountBrandingStateProvider(IAccountBrandingResolver resolver)
    {
        _resolver = resolver;
    }

    public Task<AccountBrandingViewModel> GetBrandingAsync(
        string? returnUrl,
        string? clientIdQuery,
        CancellationToken cancellationToken = default)
    {
        var key = $"{returnUrl}\0{clientIdQuery}";
        if (_brandingTask is not null && _cacheKey == key)
        {
            return _brandingTask;
        }

        _cacheKey = key;
        _brandingTask = _resolver.ResolveAsync(returnUrl, clientIdQuery, cancellationToken);
        return _brandingTask;
    }
}
