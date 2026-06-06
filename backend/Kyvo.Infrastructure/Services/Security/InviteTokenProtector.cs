using Kyvo.Application.Services.Security;
using Microsoft.AspNetCore.DataProtection;

namespace Kyvo.Infrastructure.Services.Security;

public sealed class InviteTokenProtector : IInviteTokenProtector
{
    public const string ProtectorPurpose = "Kyvo.TenantInvite.Token";
    public const string Prefix = "inv:v1:";

    private readonly IDataProtector _protector;

    public InviteTokenProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(ProtectorPurpose);
    }

    public string Protect(string plaintextToken)
    {
        ArgumentNullException.ThrowIfNull(plaintextToken);
        return Prefix + _protector.Protect(plaintextToken);
    }

    public string Unprotect(string protectedToken)
    {
        ArgumentNullException.ThrowIfNull(protectedToken);
        if (!protectedToken.StartsWith(Prefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invite token payload is not protected.");
        }

        return _protector.Unprotect(protectedToken[Prefix.Length..]);
    }
}
