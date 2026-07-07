using OpenIddict.EntityFrameworkCore.Models;

namespace Kyvo.Infrastructure.Persistence.Entities;

public sealed class KyvoOpenIddictApplication
    : OpenIddictEntityFrameworkCoreApplication<Guid, KyvoOpenIddictAuthorization, KyvoOpenIddictToken>
{
    public Guid ApplicationId { get; set; }
    public bool IsSystem { get; set; }
    public int AccessTokenTtlSeconds { get; set; } = 900;
}
