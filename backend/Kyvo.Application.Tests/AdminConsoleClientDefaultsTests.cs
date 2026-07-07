using Kyvo.Application.Common;
using Kyvo.Domain.Constants;
using Xunit;

namespace Kyvo.Application.Tests;

public sealed class AdminConsoleClientDefaultsTests
{
    [Fact]
    public void BuildRedirectUris_includes_issuer_callback_and_defaults()
    {
        var uris = AdminConsoleClientDefaults.BuildRedirectUris("https://idp.example");

        Assert.Contains("https://idp.example/auth/callback", uris);
        foreach (var defaultUri in PlatformDefaults.AdminConsole.DefaultRedirectUris)
        {
            Assert.Contains(defaultUri, uris);
        }
    }

    [Fact]
    public void BuildPostLogoutRedirectUris_includes_issuer_login_and_defaults()
    {
        var uris = AdminConsoleClientDefaults.BuildPostLogoutRedirectUris("https://idp.example/");

        Assert.Contains("https://idp.example/login", uris);
        foreach (var defaultUri in PlatformDefaults.AdminConsole.DefaultPostLogoutRedirectUris)
        {
            Assert.Contains(defaultUri, uris);
        }
    }
}
