using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuranCompanion.Domain.Entities;

namespace QuranCompanion.Infrastructure.Persistence.Configurations;

public class EncouragementConfiguration : IEntityTypeConfiguration<Encouragement>
{
    public void Configure(EntityTypeBuilder<Encouragement> builder)
    {
        builder.ToTable("Encouragements");
        builder.Property(e => e.Message).IsRequired().HasMaxLength(200);

        builder.HasOne(e => e.FromUser)
            .WithMany()
            .HasForeignKey(e => e.FromUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ToUser)
            .WithMany()
            .HasForeignKey(e => e.ToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ToUserId, e.CreatedAtUtc });
    }
}
