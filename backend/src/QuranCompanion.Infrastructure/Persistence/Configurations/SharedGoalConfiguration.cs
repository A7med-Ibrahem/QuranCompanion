using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuranCompanion.Domain.Entities;

namespace QuranCompanion.Infrastructure.Persistence.Configurations;

public class SharedGoalConfiguration : IEntityTypeConfiguration<SharedGoal>
{
    public void Configure(EntityTypeBuilder<SharedGoal> builder)
    {
        builder.ToTable("SharedGoals");
        builder.HasKey(g => g.GroupId);

        builder.HasOne(g => g.Group)
            .WithOne()
            .HasForeignKey<SharedGoal>(g => g.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.CreatedByUser)
            .WithMany()
            .HasForeignKey(g => g.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
