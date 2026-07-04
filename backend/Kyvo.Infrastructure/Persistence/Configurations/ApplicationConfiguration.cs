using Kyvo.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AppEntity = Kyvo.Domain.Entities.Application;

namespace Kyvo.Infrastructure.Persistence.Configurations;

public sealed class ApplicationConfiguration : BaseEntityConfiguration<AppEntity>
{
    public override void Configure(EntityTypeBuilder<AppEntity> builder)
    {
        base.Configure(builder);

        builder.ToTable("applications");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasColumnName("slug")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(x => x.IsSystem)
            .HasColumnName("is_system")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.BrandingEnabled)
            .HasColumnName("branding_enabled")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.BrandingPrimaryColor)
            .HasColumnName("branding_primary_color")
            .HasMaxLength(7);

        builder.Property(x => x.BrandingSecondaryColor)
            .HasColumnName("branding_secondary_color")
            .HasMaxLength(7);

        builder.Property(x => x.BrandingLogoPath)
            .HasColumnName("branding_logo_path")
            .HasMaxLength(260);

        builder.Property(x => x.BrandingHeroTitle)
            .HasColumnName("branding_hero_title")
            .HasMaxLength(AppEntity.BRANDING_HERO_TITLE_MAX_LENGTH);

        builder.Property(x => x.BrandingHeroSubtitle)
            .HasColumnName("branding_hero_subtitle")
            .HasMaxLength(AppEntity.BRANDING_HERO_SUBTITLE_MAX_LENGTH);

        builder.HasIndex(x => x.Slug)
            .IsUnique();
    }
}
