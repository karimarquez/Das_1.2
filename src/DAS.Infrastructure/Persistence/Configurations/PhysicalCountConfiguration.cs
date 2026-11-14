using DAS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAS.Infrastructure.Persistence.Configurations;

public sealed class PhysicalCountConfiguration : IEntityTypeConfiguration<PhysicalCount>
{
    public void Configure(EntityTypeBuilder<PhysicalCount> builder)
    {
        builder.HasMany(count => count.Lines).WithOne()
            .HasForeignKey("PhysicalCountId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PhysicalCountLineConfiguration : IEntityTypeConfiguration<PhysicalCountLine>
{
    public void Configure(EntityTypeBuilder<PhysicalCountLine> builder)
    {
        builder.Property<int>("Id");
        builder.HasKey("Id");
        builder.HasOne<InventoryProduct>().WithMany()
            .HasForeignKey(line => line.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(line => line.TheoreticalStock).HasPrecision(18, 4);
        builder.Property(line => line.CountedStock).HasPrecision(18, 4);
        builder.Property(line => line.UnitCost).HasPrecision(18, 2);
        builder.Ignore(line => line.Difference);
    }
}
