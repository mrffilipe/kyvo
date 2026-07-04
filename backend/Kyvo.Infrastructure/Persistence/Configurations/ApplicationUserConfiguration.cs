using Kyvo.Domain.Entities;
using Kyvo.Domain.ValueObjects;
using Kyvo.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kyvo.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="ApplicationUser"/> to the <c>users</c> table (ASP.NET Core Identity + Kyvo profile columns).
/// </summary>
public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("users");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.UserName)
            .HasColumnName("user_name")
            .HasMaxLength(255);

        builder.Property(x => x.NormalizedUserName)
            .HasColumnName("normalized_user_name")
            .HasMaxLength(255);

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(255);

        builder.Property(x => x.EmailConfirmed)
            .HasColumnName("email_confirmed");

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash");

        builder.Property(x => x.SecurityStamp)
            .HasColumnName("security_stamp");

        builder.Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp");

        builder.Property(x => x.PhoneNumber)
            .HasColumnName("phone_number");

        builder.Property(x => x.PhoneNumberConfirmed)
            .HasColumnName("phone_number_confirmed");

        builder.Property(x => x.TwoFactorEnabled)
            .HasColumnName("two_factor_enabled");

        builder.Property(x => x.LockoutEnd)
            .HasColumnName("lockout_end");

        builder.Property(x => x.LockoutEnabled)
            .HasColumnName("lockout_enabled");

        builder.Property(x => x.AccessFailedCount)
            .HasColumnName("access_failed_count");

        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(120)
            .IsRequired();

        builder.OwnsOne(
            x => x.PhotoUrl,
            b => b.Property(y => y.Value)
                .HasColumnName("photo_url")
                .HasMaxLength(PhotoUrl.MAX_LENGTH));

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
