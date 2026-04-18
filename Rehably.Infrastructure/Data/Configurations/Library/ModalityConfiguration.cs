using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehably.Domain.Entities.Library;

namespace Rehably.Infrastructure.Data.Configurations.Library;

public class ModalityConfiguration : IEntityTypeConfiguration<Modality>
{
    public void Configure(EntityTypeBuilder<Modality> builder)
    {
        builder.ToTable("Modalities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.NameArabic)
            .HasMaxLength(200);

        builder.Property(x => x.TherapeuticCategory)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.MainGoal)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ParametersNotes)
            .HasMaxLength(2000);

        builder.Property(x => x.ClinicalNotes)
            .HasMaxLength(2000);

        builder.Property(x => x.RelatedConditionCodes)
            .HasMaxLength(500);

        builder.HasIndex(x => x.Code)
            .HasFilter("\"ClinicId\" IS NULL")
            .IsUnique();

        builder.HasIndex(x => x.ClinicId);
        builder.HasIndex(x => x.ModalityType);
        builder.HasIndex(x => x.AccessTier);
        builder.HasIndex(x => x.IsDeleted);

        builder.HasOne(x => x.Clinic)
            .WithMany()
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
