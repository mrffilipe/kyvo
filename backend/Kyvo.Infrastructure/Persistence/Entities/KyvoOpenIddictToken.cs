using OpenIddict.EntityFrameworkCore.Models;

namespace Kyvo.Infrastructure.Persistence.Entities;

public sealed class KyvoOpenIddictToken
    : OpenIddictEntityFrameworkCoreToken<Guid, KyvoOpenIddictApplication, KyvoOpenIddictAuthorization>
{
}
