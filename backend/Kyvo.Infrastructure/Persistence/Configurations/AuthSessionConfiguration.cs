using Kyvo.Domain.Entities;
using Kyvo.Infrastructure.Common;
using Kyvo.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kyvo.Infrastructure.Persistence.Configurations;

public sealed class AuthSessionConfiguration : BaseEntityConfiguration<AuthSession>
{
    public override void Configure(EntityTypeBuilder<AuthSession> builder)
    {
        base.Configure(builder);

        builder.ToTable("auth_sessions");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id");

        builder.Property(x => x.MembershipId)
            .HasColumnName("membership_id");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(x => x.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(500);

        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(120);

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(x => x.LastActivityAt)
            .HasColumnName("last_activity_at")
            .IsRequired();

        builder.Ignore(x => x.User);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Membership)
            .WithMany()
            .HasForeignKey(x => x.MembershipId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
