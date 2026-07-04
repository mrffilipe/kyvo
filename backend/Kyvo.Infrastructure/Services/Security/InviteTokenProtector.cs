using Kyvo.Application.Services.Security;
using Microsoft.AspNetCore.DataProtection;

namespace Kyvo.Infrastructure.Services.Security;

public sealed class InviteTokenProtector : IInviteTokenProtector
{
    public const string PROTECTOR_PURPOSE = "Kyvo.TenantInvite.Token";
    public const string PREFIX = "inv:v1:";

    private readonly IDataProtector _protector;

    public InviteTokenProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(PROTECTOR_PURPOSE);
    }

    public string Protect(string plaintextToken)
    {
        ArgumentNullException.ThrowIfNull(plaintextToken);
        return PREFIX + _protector.Protect(plaintextToken);
    }

    public string Unprotect(string protectedToken)
    {
        ArgumentNullException.ThrowIfNull(protectedToken);
        if (!protectedToken.StartsWith(PREFIX, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invite token payload is not protected.");
        }

        return _protector.Unprotect(protectedToken[PREFIX.Length..]);
    }
}
