using Kyvo.Application.Services.Auth;
using Kyvo.Application.Services.Registration;

namespace Kyvo.API.Common;

internal static class RegisterUserResultExtensions
{
    public static ExternalLoginResult ToExternalLoginResult(this RegisterUserResult result) =>
        new()
        {
            UserId = result.UserId,
            Email = result.Email,
            DisplayName = result.DisplayName,
            PlatformRoles = [],
            TenantMemberships = []
        };
}
