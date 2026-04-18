using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehably.Domain.Entities.Library;

namespace Rehably.Infrastructure.Data.Configurations.Library;

public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.ToTable("Assessments");

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

        builder.Property(x => x.Instructions)
            .HasMaxLength(2000);

        builder.Property(x => x.ScoringGuide)
            .HasMaxLength(2000);

        builder.Property(x => x.RelatedConditionCodes)
            .HasMaxLength(500);

        builder.HasIndex(x => x.Code)
            .HasFilter("\"ClinicId\" IS NULL")
            .IsUnique();

        builder.HasIndex(x => x.ClinicId);
        builder.HasIndex(x => x.TimePoint);
        builder.HasIndex(x => x.AccessTier);
        builder.HasIndex(x => x.IsDeleted);

        builder.HasOne(x => x.Clinic)
            .WithMany()
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
