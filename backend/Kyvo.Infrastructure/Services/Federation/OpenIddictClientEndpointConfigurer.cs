using Kyvo.Domain.Enums;
using Kyvo.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using OpenIddict.Client;

namespace Kyvo.Infrastructure.Services.Federation;

/// <summary>
/// Populates OpenIddict.Client redirection endpoint URIs from admin-configured federated identity providers.
/// Dynamic registrations resolved by <see cref="DynamicOpenIddictClientService"/> are not present in
/// <c>options.Registrations</c> at startup, so OpenIddict would otherwise fail validation when the
/// authorization code flow is enabled.
/// </summary>
public sealed class OpenIddictClientEndpointConfigurer :
    IPostConfigureOptions<OpenIddictClientOptions>,
    IOptionsChangeTokenSource<OpenIddictClientOptions>
{
    internal const string RedirectionPlaceholderPath = "/callback/login/_";
    internal const string PostLogoutPlaceholderPath = "/callback/logout/_";

    private readonly IServiceProvider _provider;
    private CancellationTokenSource _reloadTokenSource = new();

    public OpenIddictClientEndpointConfigurer(IServiceProvider provider) =>
        _provider = provider;

    public void PostConfigure(string? name, OpenIddictClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        using var scope = _provider.CreateScope();
        var identityProviders = scope.ServiceProvider.GetRequiredService<IIdentityProviderRepository>();
        var federated = identityProviders.ListEnabledAsync()
            .GetAwaiter()
            .GetResult()
            .Where(provider => provider.ProviderType != IdentityProviderType.Local)
            .ToList();

        options.RedirectionEndpointUris.Clear();
        foreach (var provider in federated)
        {
            options.RedirectionEndpointUris.Add(
                new Uri($"/callback/login/{provider.Alias}", UriKind.Relative));
        }

        if (options.RedirectionEndpointUris.Count is 0)
        {
            options.RedirectionEndpointUris.Add(new Uri(RedirectionPlaceholderPath, UriKind.Relative));
        }

        options.PostLogoutRedirectionEndpointUris.Clear();
        foreach (var provider in federated)
        {
            options.PostLogoutRedirectionEndpointUris.Add(
                new Uri($"/callback/logout/{provider.Alias}", UriKind.Relative));
        }

        if (options.PostLogoutRedirectionEndpointUris.Count is 0)
        {
            options.PostLogoutRedirectionEndpointUris.Add(new Uri(PostLogoutPlaceholderPath, UriKind.Relative));
        }
    }

    public void Reload()
    {
        var previous = Interlocked.Exchange(ref _reloadTokenSource, new CancellationTokenSource());
        previous.Cancel();
        previous.Dispose();
    }

    IChangeToken IOptionsChangeTokenSource<OpenIddictClientOptions>.GetChangeToken() =>
        new CancellationChangeToken(_reloadTokenSource.Token);

    string? IOptionsChangeTokenSource<OpenIddictClientOptions>.Name => Options.DefaultName;
}
