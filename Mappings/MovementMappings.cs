using DAS.Models.DTOs;
using DAS.Models.Entities;
using DAS.Models.ViewModels;

namespace DAS.Mappings;

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

    public static RegisterMovementDto ToDto(this MovementInputViewModel input) =>
        new(input.ProductId, input.Type, input.Quantity, input.Reason.Trim(), input.Date);

    public static MovementQueryDto ToQuery(this MovementFilterViewModel filter) =>
        new(filter.ProductId, filter.Type, filter.From, filter.To);
}
