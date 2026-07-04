using Kyvo.Domain.Entities;
using Kyvo.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

namespace Kyvo.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity user store mapped to the <c>users</c> table. Profile fields mirror
/// <see cref="User"/>; use <see cref="Identity.UserMapper"/> to cross the persistence/domain boundary.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = default!;
    public PhotoUrl? PhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<TenantMembership> Memberships { get; set; } = [];
    public ICollection<UserPlatformRole> PlatformRoles { get; set; } = [];

    public void SetCreatedAt()
    {
        var utcNow = DateTime.UtcNow;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void SetUpdatedAt() => UpdatedAt = DateTime.UtcNow;
}
