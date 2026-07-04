using Kyvo.Infrastructure.Identity;
using Kyvo.Infrastructure.Common;
using Kyvo.Infrastructure.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Kyvo.Infrastructure.Services.Identity;

/// <summary>
/// Enforces "password must contain at least one letter" (<see cref="PasswordPolicyOptions.RequireLetter"/>).
/// </summary>
public sealed class KyvoPasswordValidator : IPasswordValidator<ApplicationUser>
{
    private readonly PasswordPolicyOptions _options;

    public KyvoPasswordValidator(IOptions<PasswordPolicyOptions> options) => _options = options.Value;

    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        if (_options.RequireLetter && (password is null || !password.Any(char.IsLetter)))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "RequiresLetter",
                Description = InfrastructureErrorMessages.PasswordPolicy.MUST_CONTAIN_LETTER
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }
}
