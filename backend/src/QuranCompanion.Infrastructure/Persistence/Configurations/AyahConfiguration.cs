using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuranCompanion.Domain.Entities;

namespace QuranCompanion.Infrastructure.Persistence.Configurations;

public class AyahConfiguration : IEntityTypeConfiguration<Ayah>
{
    public void Configure(EntityTypeBuilder<Ayah> builder)
    {
        builder.ToTable("Ayahs");
        builder.Property(a => a.Text).IsRequired();

        builder.HasOne(a => a.Surah)
            .WithMany(s => s.Ayahs)
            .HasForeignKey(a => a.SurahNumber)
            .OnDelete(DeleteBehavior.Restrict);

        // One ayah per (surah, position) - prevents duplicate imports.
        builder.HasIndex(a => new { a.SurahNumber, a.NumberInSurah }).IsUnique();
        builder.HasIndex(a => a.GlobalNumber).IsUnique();
    }
}
