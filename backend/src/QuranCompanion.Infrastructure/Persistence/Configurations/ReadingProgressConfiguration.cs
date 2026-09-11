using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuranCompanion.Domain.Entities;

namespace QuranCompanion.Infrastructure.Persistence.Configurations;

public class ReadingProgressConfiguration : IEntityTypeConfiguration<ReadingProgress>
{
    public void Configure(EntityTypeBuilder<ReadingProgress> builder)
    {
        builder.ToTable("ReadingProgress");
        builder.HasKey(p => p.UserId); // one row per user

        builder.HasOne(p => p.User)
            .WithOne()
            .HasForeignKey<ReadingProgress>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
