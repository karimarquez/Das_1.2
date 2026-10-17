using DAS.Models.Enums;

namespace DAS.Models.DTOs;

public sealed record MovementDto
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public MovementType Type { get; init; }
    public decimal Quantity { get; init; }
    public string Unit { get; init; } = string.Empty;
    public decimal StockBefore { get; init; }
    public decimal StockAfter { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public DateTime RecordedAt { get; init; }

    public bool IsIncrease => StockAfter > StockBefore;
}

public sealed record RegisterMovementDto(int ProductId, MovementType Type, decimal Quantity, string Reason, DateTime? Date);

public sealed record MovementQueryDto(int? ProductId = null, MovementType? Type = null, DateTime? From = null, DateTime? To = null, int? Take = null);
