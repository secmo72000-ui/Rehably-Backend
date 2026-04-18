using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehably.Domain.Entities.Library;

namespace Rehably.Infrastructure.Data.Configurations.Library;

public class TreatmentConfiguration : IEntityTypeConfiguration<Treatment>
{
    public void Configure(EntityTypeBuilder<Treatment> builder)
    {
        builder.ToTable("Treatments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.NameArabic)
            .HasMaxLength(200);

        builder.Property(x => x.AffectedArea)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.RedFlags)
            .HasMaxLength(1000);

        builder.Property(x => x.Contraindications)
            .HasMaxLength(1000);

        builder.Property(x => x.ClinicalNotes)
            .HasMaxLength(2000);

        builder.Property(x => x.SourceReference)
            .HasMaxLength(200);

        builder.Property(x => x.SourceDetails)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.Code)
            .HasFilter("\"ClinicId\" IS NULL")
            .IsUnique();

        builder.HasIndex(x => x.ClinicId);
        builder.HasIndex(x => x.BodyRegionCategoryId);
        builder.HasIndex(x => x.AccessTier);
        builder.HasIndex(x => x.IsDeleted);

        builder.HasOne(x => x.BodyRegionCategory)
            .WithMany(x => x.Treatments)
            .HasForeignKey(x => x.BodyRegionCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Clinic)
            .WithMany()
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Phases)
            .WithOne()
            .HasForeignKey(p => p.TreatmentCode)
            .HasPrincipalKey(t => t.Code)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
