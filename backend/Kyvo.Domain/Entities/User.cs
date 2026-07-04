using Kyvo.Domain.Common;
using Kyvo.Domain.Exceptions;
using Kyvo.Domain.ValueObjects;

namespace Kyvo.Domain.Entities;

/// <summary>
/// Kyvo platform user (profile and lifecycle). Authentication state (password hash, lockout, external logins)
/// is owned by ASP.NET Core Identity in Infrastructure (<c>ApplicationUser</c>), not this aggregate.
/// </summary>
public sealed class User : BaseEntity
{
    public string Email { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public PhotoUrl? PhotoUrl { get; private set; }
    public bool IsActive { get; private set; }

    private User()
    {
    }

    public User(EmailAddress email, string displayName, string? photoUrl = null)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainValidationException(DomainErrorMessages.User.DISPLAY_NAME_REQUIRED);
        }

        Email = email.Value;
        DisplayName = displayName.Trim();
        PhotoUrl = photoUrl;
        IsActive = true;
    }

    /// <summary>Rehydrates a user loaded from persistence (Infrastructure mapper only).</summary>
    public static User Restore(
        Guid id,
        string email,
        string displayName,
        PhotoUrl? photoUrl,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt)
    {
        var user = new User
        {
            Email = email,
            DisplayName = displayName,
            PhotoUrl = photoUrl,
            IsActive = isActive
        };

        user.RestoreIdentity(id, createdAt, updatedAt);
        return user;
    }

    public void UpdateProfile(string displayName, string? photoUrl)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainValidationException(DomainErrorMessages.User.DISPLAY_NAME_REQUIRED);
        }

        DisplayName = displayName.Trim();
        PhotoUrl = photoUrl;
    }

    public void Deactivate() => IsActive = false;
}
