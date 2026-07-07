using Kyvo.Application.Common;
using Kyvo.Domain.Exceptions;
using Xunit;

namespace Kyvo.Application.Tests;

public sealed class OAuthClientFieldValidatorTests
{
    [Fact]
    public void ParseAndValidateRedirectUris_accepts_absolute_https_uri()
    {
        var uris = OAuthClientFieldValidator.ParseAndValidateRedirectUris("https://app.example/callback");

        Assert.Single(uris);
        Assert.Equal("https://app.example/callback", uris[0]);
    }

    [Fact]
    public void ParseAndValidateRedirectUris_rejects_relative_uri()
    {
        Assert.Throws<DomainValidationException>(() =>
            OAuthClientFieldValidator.ParseAndValidateRedirectUris("/callback"));
    }

    [Fact]
    public void ParseAndValidateAllowedScopes_accepts_openid_and_profile()
    {
        var scopes = OAuthClientFieldValidator.ParseAndValidateAllowedScopes("openid profile", null);

        Assert.Equal(2, scopes.Count);
        Assert.Contains("openid", scopes);
        Assert.Contains("profile", scopes);
    }
}
