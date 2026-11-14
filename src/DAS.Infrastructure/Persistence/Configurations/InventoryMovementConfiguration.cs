using DAS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAS.Infrastructure.Persistence.Configurations;

public sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        //Restrict: un producto con historial no se puede borrar, solo desactivar
        builder.HasOne<InventoryProduct>().WithMany()
            .HasForeignKey(movement => movement.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(movement => movement.Quantity).HasPrecision(18, 4);
        builder.Property(movement => movement.StockBefore).HasPrecision(18, 4);
        builder.Property(movement => movement.StockAfter).HasPrecision(18, 4);
    }
}
