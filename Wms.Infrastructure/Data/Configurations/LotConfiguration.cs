// Wms.Infrastructure/Data/Configurations/LotConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Data.Configurations;

public class LotConfiguration : IEntityTypeConfiguration<Lot>
{
    public void Configure(EntityTypeBuilder<Lot> builder)
    {
        builder.ToTable("Lots");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Number)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ExpiryDate);
        builder.Property(e => e.ManufacturedDate);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt);

        // Relationships
        builder.HasOne(e => e.Item)
            .WithMany()
            .HasForeignKey(e => e.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => new { e.ItemId, e.Number }).IsUnique();
        builder.HasIndex(e => e.ExpiryDate);
        builder.HasIndex(e => e.IsActive);
    }
}