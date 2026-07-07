using OpenIddict.EntityFrameworkCore.Models;

namespace Kyvo.Infrastructure.Persistence.Entities;

public sealed class KyvoOpenIddictAuthorization
    : OpenIddictEntityFrameworkCoreAuthorization<Guid, KyvoOpenIddictApplication, KyvoOpenIddictToken>
{
}
