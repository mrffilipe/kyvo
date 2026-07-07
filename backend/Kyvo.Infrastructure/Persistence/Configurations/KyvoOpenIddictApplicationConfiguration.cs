using Kyvo.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kyvo.Infrastructure.Persistence.Configurations;

public sealed class KyvoOpenIddictApplicationConfiguration : IEntityTypeConfiguration<KyvoOpenIddictApplication>
{
    public void Configure(EntityTypeBuilder<KyvoOpenIddictApplication> builder)
    {
        builder.Property(x => x.ApplicationId)
            .HasColumnName("application_id")
            .IsRequired();

        builder.Property(x => x.IsSystem)
            .HasColumnName("is_system")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.AccessTokenTtlSeconds)
            .HasColumnName("access_token_ttl_seconds")
            .IsRequired()
            .HasDefaultValue(900);

        builder.HasIndex(x => x.ApplicationId);
    }
}
