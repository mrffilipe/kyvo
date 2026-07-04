using Kyvo.Domain.Entities;
using Kyvo.Domain.ValueObjects;

namespace Kyvo.Infrastructure.Identity;

public static class UserMapper
{
    public static User ToDomain(ApplicationUser entity) =>
        User.Restore(
            entity.Id,
            entity.Email ?? string.Empty,
            entity.DisplayName,
            entity.PhotoUrl,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt);

    public static ApplicationUser ToNewPersistence(User domain)
    {
        var entity = new ApplicationUser
        {
            Id = domain.Id,
            Email = domain.Email,
            UserName = domain.Email,
            DisplayName = domain.DisplayName,
            PhotoUrl = domain.PhotoUrl,
            IsActive = domain.IsActive,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt
        };

        return entity;
    }

    public static void ApplyToPersistence(User domain, ApplicationUser entity)
    {
        entity.Email = domain.Email;
        entity.UserName = domain.Email;
        entity.DisplayName = domain.DisplayName;
        entity.PhotoUrl = domain.PhotoUrl;
        entity.IsActive = domain.IsActive;
        entity.UpdatedAt = domain.UpdatedAt;
    }
}
