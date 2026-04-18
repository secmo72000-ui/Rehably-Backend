using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehably.Domain.Entities.Library;

namespace Rehably.Infrastructure.Data.Configurations.Library;

public class BodyRegionCategoryConfiguration : IEntityTypeConfiguration<BodyRegionCategory>
{
    public void Configure(EntityTypeBuilder<BodyRegionCategory> builder)
    {
        builder.ToTable("BodyRegionCategories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.NameArabic)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.HasIndex(x => x.DisplayOrder);

        builder.HasData(
            new BodyRegionCategory { Id = new Guid("00000000-0000-0000-0000-000000000001"), Code = "cervical", Name = "Cervical Spine", NameArabic = "العمود الفقري العنقي", DisplayOrder = 1, IsActive = true },
            new BodyRegionCategory { Id = new Guid("00000000-0000-0000-0000-000000000002"), Code = "thoracic", Name = "Thoracic Spine", NameArabic = "العمود الفقري الصدري", DisplayOrder = 2, IsActive = true },
            new BodyRegionCategory { Id = new Guid("00000000-0000-0000-0000-000000000003"), Code = "lumbar", Name = "Lumbar Spine", NameArabic = "العمود الفقري القطني", DisplayOrder = 3, IsActive = true },
            new BodyRegionCategory { Id = new Guid("00000000-0000-0000-0000-000000000004"), Code = "shoulder", Name = "Shoulder", NameArabic = "الكتف", DisplayOrder = 4, IsActive = true },
            new BodyRegionCategory { Id = new Guid("00000000-0000-0000-0000-000000000005"), Code = "elbow", Name = "Elbow", NameArabic = "الكوع", DisplayOrder = 5, IsActive = true },
            new BodyRegionCategory { Id = new Guid("00000000-0000-0000-0000-000000000006"), Code = "wrist_hand", Name = "Wrist/Hand", NameArabic = "الرسغ/اليد", DisplayOrder = 6, IsActive = true },
            new BodyRegionCategory { Id = new Guid("00000000-0000-0000-0000-000000000007"), Code = "hip", Name = "Hip", NameArabic = "الورك", DisplayOrder = 7, IsActive = true },
            new BodyRegionCategory { Id = new Guid("00000000-0000-0000-0000-000000000008"), Code = "knee", Name = "Knee", NameArabic = "الركبة", DisplayOrder = 8, IsActive = true },
            new BodyRegionCategory { Id = new Guid("00000000-0000-0000-0000-000000000009"), Code = "ankle_foot", Name = "Ankle/Foot", NameArabic = "الكاحل/القدم", DisplayOrder = 9, IsActive = true }
        );
    }
}
