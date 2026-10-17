namespace DAS.Models.Entities;

public class PhysicalCount
{
    public int Id { get; set; }
    public DateTime CountedAt { get; set; }
    public string UserName { get; set; } = string.Empty;
    public List<PhysicalCountLine> Lines { get; set; } = [];
}

public class PhysicalCountLine
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal TheoreticalStock { get; set; }
    public decimal CountedStock { get; set; }
    public decimal UnitCost { get; set; }

    public decimal Difference => CountedStock - TheoreticalStock;
}
