namespace DAS.Models.DTOs;

public sealed record CountLineDto(int ProductId, decimal CountedStock);

public sealed record PhysicalCountLineDto
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public decimal TheoreticalStock { get; init; }
    public decimal CountedStock { get; init; }
    public decimal Difference { get; init; }
    public decimal DifferenceCost { get; init; }
}

public sealed record PhysicalCountDto
{
    public int Id { get; init; }
    public DateTime CountedAt { get; init; }
    public string UserName { get; init; } = string.Empty;
    public IReadOnlyList<PhysicalCountLineDto> Lines { get; init; } = [];
}
