using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehably.Domain.Entities.Library;

namespace Rehably.Infrastructure.Data.Configurations.Library;

public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("Exercises");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.NameArabic)
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.RelatedConditionCode)
            .HasMaxLength(20);

        builder.Property(x => x.Tags)
            .HasMaxLength(500);

        builder.Property(x => x.VideoUrl)
            .HasMaxLength(500);

        builder.Property(x => x.ThumbnailUrl)
            .HasMaxLength(500);

        builder.Property(x => x.LinkedExerciseIds)
            .HasMaxLength(200);

        builder.HasIndex(x => x.ClinicId);
        builder.HasIndex(x => x.BodyRegionCategoryId);
        builder.HasIndex(x => x.RelatedConditionCode);
        builder.HasIndex(x => x.AccessTier);
        builder.HasIndex(x => x.IsDeleted);

        builder.HasOne(x => x.BodyRegionCategory)
            .WithMany(x => x.Exercises)
            .HasForeignKey(x => x.BodyRegionCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Clinic)
            .WithMany()
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
