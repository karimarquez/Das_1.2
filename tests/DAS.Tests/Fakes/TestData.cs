using DAS.Application.DTOs;
using DAS.Domain.Entities;
using DAS.Domain.Enums;

namespace DAS.Tests.Fakes;

public static class TestData
{
    public const string UserName = "encargado@pipiris.test";
    public static readonly DateTime Now = new(2026, 11, 14, 15, 30, 0);

    //La existencia inicial entra por un movimiento, igual que en la aplicación
    public static InventoryProduct Product(
        int id,
        string name,
        decimal stock = 0,
        decimal minimum = 0,
        string unit = "botellas",
        string baseUnit = "botellas",
        decimal equivalence = 1,
        decimal cost = 1m,
        bool active = true)
    {
        var product = new InventoryProduct
        {
            Id = id,
            Name = name,
            Category = "Licores",
            Presentation = "Botella",
            Unit = unit,
            BaseUnit = baseUnit,
            BaseUnitsPerStockUnit = equivalence,
            MinimumStock = minimum,
            UnitCost = cost,
            Location = "Barra"
        };

        if (stock > 0)
        {
            product.RegisterMovement(MovementType.Entrada, stock, "Existencia inicial", "semilla", Now.AddDays(-30));
        }

        product.IsActive = active;
        return product;
    }

    public static SaveProductDto NewProduct(string name, decimal initialStock = 0) => new()
    {
        Name = name,
        Category = "Licores",
        Presentation = "Botella 750 ml",
        Unit = "botellas",
        BaseUnit = "oz",
        BaseUnitsPerStockUnit = 25.36m,
        MinimumStock = 2,
        UnitCost = 15m,
        Location = "Estante de licores",
        InitialStock = initialStock
    };
}
