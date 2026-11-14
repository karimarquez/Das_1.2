using DAS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAS.Infrastructure.Persistence.Configurations;

public sealed class InventoryProductConfiguration : IEntityTypeConfiguration<InventoryProduct>
{
    public void Configure(EntityTypeBuilder<InventoryProduct> builder)
    {
        builder.HasIndex(product => product.Name).IsUnique();
        builder.Property(product => product.Name).HasMaxLength(80).IsRequired();
        builder.Property(product => product.Stock).HasPrecision(18, 4);
        builder.Property(product => product.MinimumStock).HasPrecision(18, 4);
        builder.Property(product => product.UnitCost).HasPrecision(18, 2);
        builder.Property(product => product.BaseUnitsPerStockUnit).HasPrecision(18, 4);
        builder.Ignore(product => product.IsLowStock);
    }
}
