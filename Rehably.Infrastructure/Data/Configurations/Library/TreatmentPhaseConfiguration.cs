using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehably.Domain.Entities.Library;

namespace Rehably.Infrastructure.Data.Configurations.Library;

public class TreatmentPhaseConfiguration : IEntityTypeConfiguration<TreatmentPhase>
{
    public void Configure(EntityTypeBuilder<TreatmentPhase> builder)
    {
        builder.ToTable("TreatmentPhases");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TreatmentCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.NameArabic)
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.MainGoal)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ClinicalNotes)
            .HasMaxLength(2000);

        builder.HasIndex(x => new { x.TreatmentCode, x.PhaseNumber })
            .HasFilter("\"ClinicId\" IS NULL")
            .IsUnique();

        builder.HasIndex(x => x.ClinicId);
        builder.HasIndex(x => x.TreatmentCode);
        builder.HasIndex(x => x.IsDeleted);

        builder.HasOne(x => x.Clinic)
            .WithMany()
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
