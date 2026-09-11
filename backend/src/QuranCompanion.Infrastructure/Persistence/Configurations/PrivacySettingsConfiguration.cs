using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuranCompanion.Domain.Entities;

namespace QuranCompanion.Infrastructure.Persistence.Configurations;

public class PrivacySettingsConfiguration : IEntityTypeConfiguration<PrivacySettings>
{
    public void Configure(EntityTypeBuilder<PrivacySettings> builder)
    {
        builder.ToTable("PrivacySettings");
        builder.HasKey(p => p.UserId);

        builder.HasOne(p => p.User)
            .WithOne()
            .HasForeignKey<PrivacySettings>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
