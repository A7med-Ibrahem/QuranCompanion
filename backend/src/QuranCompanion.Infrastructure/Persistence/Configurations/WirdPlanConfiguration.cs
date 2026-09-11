using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuranCompanion.Domain.Entities;

namespace QuranCompanion.Infrastructure.Persistence.Configurations;

public class WirdPlanConfiguration : IEntityTypeConfiguration<WirdPlan>
{
    public void Configure(EntityTypeBuilder<WirdPlan> builder)
    {
        builder.ToTable("WirdPlans");
        builder.HasKey(p => p.UserId);

        builder.HasOne(p => p.User)
            .WithOne()
            .HasForeignKey<WirdPlan>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
