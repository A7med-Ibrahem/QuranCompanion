using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuranCompanion.Domain.Entities;

namespace QuranCompanion.Infrastructure.Persistence.Configurations;

public class SurahConfiguration : IEntityTypeConfiguration<Surah>
{
    public void Configure(EntityTypeBuilder<Surah> builder)
    {
        builder.ToTable("Surahs");
        builder.HasKey(s => s.Number);
        builder.Property(s => s.Number).ValueGeneratedNever();
        builder.Property(s => s.ArabicName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.EnglishName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.EnglishNameTranslation).IsRequired().HasMaxLength(150);
    }
}
