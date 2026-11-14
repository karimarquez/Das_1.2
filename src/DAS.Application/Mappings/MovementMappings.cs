using DAS.Application.DTOs;
using DAS.Domain.Entities;

namespace DAS.Application.Mappings;

public static class MovementMappings
{
    public static MovementDto ToDto(this InventoryMovement movement) => new()
    {
        Id = movement.Id,
        ProductId = movement.ProductId,
        ProductName = movement.ProductName,
        Type = movement.Type,
        Quantity = movement.Quantity,
        Unit = movement.Unit,
        StockBefore = movement.StockBefore,
        StockAfter = movement.StockAfter,
        Reason = movement.Reason,
        UserName = movement.UserName,
        RecordedAt = movement.RecordedAt
    };
}
