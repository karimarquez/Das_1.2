using DAS.Models.Enums;

namespace DAS.Models.Catalogs;

public static class InventoryCatalog
{
    public static readonly IReadOnlyList<string> Categories =
    [
        "Cervezas y sodas",
        "Licores",
        "Zumos y jugos",
        "Lácteos",
        "Frutas e insumos",
        "Desechables"
    ];

    public static readonly IReadOnlyList<string> StockUnits =
    [
        "botellas",
        "latas",
        "galones",
        "cajas",
        "bolsas",
        "paquetes",
        "libras",
        "unidades"
    ];

    public static readonly IReadOnlyList<string> BaseUnits =
    [
        "oz",
        "ml",
        "lb",
        "unidades",
        "botellas",
        "latas"
    ];

    public static IReadOnlyList<string> For(CatalogKind kind) => kind switch
    {
        CatalogKind.Category => Categories,
        CatalogKind.StockUnit => StockUnits,
        CatalogKind.BaseUnit => BaseUnits,
        _ => []
    };
}
