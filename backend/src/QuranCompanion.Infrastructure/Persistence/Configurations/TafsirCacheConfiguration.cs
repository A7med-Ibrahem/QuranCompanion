using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuranCompanion.Domain.Entities;

namespace QuranCompanion.Infrastructure.Persistence.Configurations;

public class TafsirCacheConfiguration : IEntityTypeConfiguration<TafsirCache>
{
    public void Configure(EntityTypeBuilder<TafsirCache> builder)
    {
        builder.ToTable("TafsirCache");
        builder.Property(t => t.Text).IsRequired();
        builder.Property(t => t.SourceName).IsRequired().HasMaxLength(200);

        builder.HasIndex(t => new { t.SurahNumber, t.AyahNumber, t.TafsirId }).IsUnique();
    }
}
