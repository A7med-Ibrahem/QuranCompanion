using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuranCompanion.Domain.Entities;

namespace QuranCompanion.Infrastructure.Persistence.Configurations;

public class WirdCompletionConfiguration : IEntityTypeConfiguration<WirdCompletion>
{
    public void Configure(EntityTypeBuilder<WirdCompletion> builder)
    {
        builder.ToTable("WirdCompletions");

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.UserId, c.CompletionDate }).IsUnique();
    }
}
