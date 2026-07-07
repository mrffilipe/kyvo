using Kyvo.Domain.Entities;
using Kyvo.Domain.Repositories;
using Kyvo.Domain.ValueObjects;
using Kyvo.Infrastructure.Oidc;
using Moq;
using Xunit;

namespace Kyvo.Application.Tests;

public sealed class OidcClaimsPrincipalFactoryTests
{
    [Fact]
    public async Task CreateAsync_IncludesSid_WithoutTenantClaims()
    {
        var platformRoles = new Mock<IUserPlatformRoleRepository>();
        platformRoles.Setup(x => x.ListByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var factory = new OidcClaimsPrincipalFactory(platformRoles.Object);
        var userId = Guid.NewGuid();
        var user = User.Restore(userId, "test@example.com", "Test User", null, true, DateTime.UtcNow, DateTime.UtcNow);
        var session = new AuthSession(userId, null, null, DateTime.UtcNow.AddHours(1), "agent", "127.0.0.1");

        var principal = await factory.CreateAsync(user, session, "kyvo-spa", ["openid", "profile", "email"]);

        Assert.Equal(session.Id.ToString("D"), principal.FindFirst("sid")?.Value);
        Assert.Null(principal.FindFirst("tid"));
        Assert.Null(principal.FindFirst("mid"));
        Assert.Empty(principal.FindAll("trole"));
    }
}
