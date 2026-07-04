using System.Security.Cryptography;
using System.Text;
using Kyvo.Application.Services.Security;

namespace Kyvo.Infrastructure.Services.Security;

public sealed class InviteTokenHasher : IInviteTokenHasher
{
    public string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
