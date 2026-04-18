using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehably.Domain.Entities.Library;

namespace Rehably.Infrastructure.Data.Configurations.Library;

public class ClinicLibraryOverrideConfiguration : IEntityTypeConfiguration<ClinicLibraryOverride>
{
    public void Configure(EntityTypeBuilder<ClinicLibraryOverride> builder)
    {
        builder.ToTable("ClinicLibraryOverrides");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OverrideDataJson)
            .HasColumnType("jsonb");

        builder.HasIndex(x => new { x.ClinicId, x.LibraryType, x.GlobalItemId })
            .IsUnique();

        builder.HasIndex(x => x.ClinicId);
        builder.HasIndex(x => x.LibraryType);
        builder.HasIndex(x => x.IsDeleted);

        builder.HasOne(x => x.Clinic)
            .WithMany()
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
