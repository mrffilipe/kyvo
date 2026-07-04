using Kyvo.Domain.Common;
using Kyvo.Domain.Exceptions;

namespace Kyvo.Domain.Entities;

public sealed class UserPlatformRole : BaseEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public Guid RoleId { get; private set; }
    public PlatformRole Role { get; private set; } = null!;

    private UserPlatformRole()
    {
    }

    public UserPlatformRole(Guid userId, Guid roleId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainValidationException(DomainErrorMessages.UserPlatformRole.USER_ID_REQUIRED);
        }

        if (roleId == Guid.Empty)
        {
            throw new DomainValidationException(DomainErrorMessages.UserPlatformRole.ROLE_ID_REQUIRED);
        }

        UserId = userId;
        RoleId = roleId;
    }
}
