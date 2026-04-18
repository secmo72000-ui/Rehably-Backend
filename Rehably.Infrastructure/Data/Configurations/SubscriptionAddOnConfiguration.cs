using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Data.Configurations;

public class SubscriptionAddOnConfiguration : IEntityTypeConfiguration<SubscriptionAddOn>
{
    public void Configure(EntityTypeBuilder<SubscriptionAddOn> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Quantity)
            .IsRequired();

        builder.Property(a => a.CalculatedPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.PriceSnapshot)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(a => a.Status)
            .IsRequired();

        builder.Property(a => a.StartDate)
            .IsRequired();

        builder.Property(a => a.EndDate)
            .IsRequired();

        builder.Property(a => a.TransactionId)
            .HasMaxLength(100);

        builder.HasOne(a => a.Subscription)
            .WithMany(s => s.AddOns)
            .HasForeignKey(a => a.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Feature)
            .WithMany()
            .HasForeignKey(a => a.FeatureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.SubscriptionId);
        builder.HasIndex(a => a.FeatureId);

        builder.HasIndex(a => new { a.SubscriptionId, a.FeatureId })
            .HasFilter($"\"Status\" = {(int)AddOnStatus.Active}")
            .IsUnique();
    }
}
