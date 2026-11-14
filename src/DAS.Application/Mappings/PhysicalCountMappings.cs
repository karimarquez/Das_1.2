using DAS.Application.DTOs;
using DAS.Domain.Entities;

namespace DAS.Application.Mappings;

public static class PhysicalCountMappings
{
    public static PhysicalCountDto ToDto(this PhysicalCount count) => new()
    {
        Id = count.Id,
        CountedAt = count.CountedAt,
        UserName = count.UserName,
        Lines = count.Lines
            .Select(line => new PhysicalCountLineDto
            {
                ProductId = line.ProductId,
                ProductName = line.ProductName,
                Unit = line.Unit,
                TheoreticalStock = line.TheoreticalStock,
                CountedStock = line.CountedStock,
                Difference = line.Difference,
                DifferenceCost = line.Difference * line.UnitCost
            })
            .ToList()
    };
}
