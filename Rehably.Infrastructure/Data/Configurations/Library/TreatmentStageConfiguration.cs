using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehably.Domain.Entities.Library;

namespace Rehably.Infrastructure.Data.Configurations.Library;

public class TreatmentStageConfiguration : IEntityTypeConfiguration<TreatmentStage>
{
    public void Configure(EntityTypeBuilder<TreatmentStage> builder)
    {
        builder.ToTable("TreatmentStages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.NameArabic)
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.BodyRegionId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();

        builder.HasOne(x => x.BodyRegion)
            .WithMany()
            .HasForeignKey(x => x.BodyRegionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
