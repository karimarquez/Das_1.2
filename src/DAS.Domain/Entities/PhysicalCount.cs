using DAS.Domain.Common;
using DAS.Domain.Exceptions;

namespace DAS.Domain.Entities;

public class PhysicalCount : Entity
{
    public DateTime CountedAt { get; set; }
    public string UserName { get; set; } = string.Empty;
    public List<PhysicalCountLine> Lines { get; set; } = [];

    //Guarda el saldo teórico del momento para compararlo con lo contado
    public PhysicalCountLine AddLine(InventoryProduct product, decimal countedStock)
    {
        if (countedStock < 0)
        {
            throw new DomainException("Las cantidades contadas no pueden ser negativas.");
        }

        var line = new PhysicalCountLine
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Unit = product.Unit,
            TheoreticalStock = product.Stock,
            CountedStock = countedStock,
            UnitCost = product.UnitCost
        };
        Lines.Add(line);
        return line;
    }
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
