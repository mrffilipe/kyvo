using Microsoft.AspNetCore.Identity;

namespace Kyvo.IDP.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public void SetCreatedAt()
    {
        var utcNow = DateTime.UtcNow;
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    public void SetUpdatedAt() => UpdatedAt = DateTime.UtcNow;
}
