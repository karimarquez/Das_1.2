using DAS.Models.Entities;
using DAS.Models.Enums;

namespace DAS.Data;

public static class InventorySeedData
{
    private const string SeedUser = "Encargado de bar";

    public static void Load(InMemoryInventoryStore store)
    {
        var createdAt = DateTime.Today.AddDays(-30);
        var products = new Dictionary<string, InventoryProduct>();

        void AddProduct(string name, string category, string presentation, string unit, string baseUnit, decimal equivalence, decimal openingStock, decimal minimum, decimal cost, string location) =>
            products[name] = store.AddProduct(new InventoryProduct
            {
                Name = name,
                Category = category,
                Presentation = presentation,
                Unit = unit,
                BaseUnit = baseUnit,
                BaseUnitsPerStockUnit = equivalence,
                Stock = openingStock,
                MinimumStock = minimum,
                UnitCost = cost,
                Location = location,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });

        AddProduct("Cerveza Pilsener", "Cervezas y sodas", "Botella 330 ml", "botellas", "botellas", 1, 2, 24, 0.85m, "Refrigerador 01");
        AddProduct("Agua mineral", "Cervezas y sodas", "Botella 600 ml", "botellas", "botellas", 1, 12, 20, 0.55m, "Refrigerador 01");
        AddProduct("Tequila blanco", "Licores", "Botella 750 ml", "botellas", "oz", 25.36m, 2, 4, 18.50m, "Estante de licores");
        AddProduct("Jugo de naranja", "Zumos y jugos", "Galón", "galones", "oz", 128, 0, 2, 4.25m, "Refrigerador 02");
        AddProduct("Leche evaporada", "Lácteos", "Lata 360 ml", "latas", "oz", 12.17m, 0, 6, 1.15m, "Refrigerador 02");
        AddProduct("Limón", "Frutas e insumos", "Unidad", "unidades", "unidades", 1, 0, 18, 0.08m, "Barra fría");
        AddProduct("Vasos acrílicos", "Desechables", "Paquete de 25", "paquetes", "unidades", 25, 0, 3, 2.40m, "Bodega de bar");
        AddProduct("Pulpa de maracuyá", "Frutas e insumos", "Bolsa de 1 lb", "bolsas", "oz", 16, 0, 4, 2.10m, "Congelador de barra");
        AddProduct("Azúcar", "Frutas e insumos", "Bolsa de 5 lb", "bolsas", "lb", 5, 0, 2, 3.75m, "Bodega de bar");
        AddProduct("Pajillas", "Desechables", "Caja de 100", "cajas", "unidades", 100, 0, 2, 1.80m, "Bodega de bar");

        var history = new (int Day, int Hour, int Minute, string Product, MovementType Type, decimal Quantity, string Reason)[]
        {
            (-7, 9, 30, "Leche evaporada", MovementType.Entrada, 12, "Compra a proveedor"),
            (-6, 10, 0, "Cerveza Pilsener", MovementType.Entrada, 24, "Compra a proveedor"),
            (-6, 10, 20, "Vasos acrílicos", MovementType.Entrada, 6, "Compra a proveedor"),
            (-6, 10, 25, "Pajillas", MovementType.Entrada, 5, "Compra a proveedor"),
            (-5, 9, 45, "Agua mineral", MovementType.Entrada, 24, "Compra a proveedor"),
            (-5, 9, 50, "Azúcar", MovementType.Entrada, 4, "Requisición de bodega"),
            (-4, 11, 0, "Tequila blanco", MovementType.Entrada, 6, "Compra a proveedor"),
            (-4, 11, 10, "Pulpa de maracuyá", MovementType.Entrada, 8, "Requisición de bodega"),
            (-3, 8, 40, "Limón", MovementType.Entrada, 50, "Requisición de medio ciento"),
            (-3, 21, 30, "Cerveza Pilsener", MovementType.Consumo, 12, "Cierre de turno"),
            (-2, 10, 15, "Jugo de naranja", MovementType.Entrada, 4, "Compra semanal"),
            (-2, 21, 40, "Agua mineral", MovementType.Consumo, 18, "Cierre de turno"),
            (-2, 21, 45, "Limón", MovementType.Consumo, 33, "Limonadas y micheladas del turno"),
            (-2, 21, 50, "Leche evaporada", MovementType.Consumo, 3, "Preparación de bebidas"),
            (-2, 21, 55, "Pajillas", MovementType.Consumo, 1, "Cierre de turno"),
            (-1, 17, 0, "Jugo de naranja", MovementType.Consumo, 1, "Preparación de bebidas"),
            (-1, 20, 30, "Cerveza Pilsener", MovementType.Merma, 2, "Botella quebrada"),
            (-1, 21, 0, "Vasos acrílicos", MovementType.Consumo, 1, "Cierre de turno"),
            (-1, 21, 10, "Pulpa de maracuyá", MovementType.Consumo, 2, "Refrescos de maracuyá"),
            (-1, 21, 15, "Azúcar", MovementType.Consumo, 1, "Refrescos y limonadas"),
            (0, 9, 12, "Cerveza Pilsener", MovementType.Entrada, 24, "Reposición de proveedor"),
            (0, 11, 38, "Tequila blanco", MovementType.Consumo, 0.5m, "Cierre de turno"),
            (0, 12, 5, "Limón", MovementType.Merma, 3, "Producto dañado")
        };

        foreach (var item in history.OrderBy(h => h.Day).ThenBy(h => h.Hour).ThenBy(h => h.Minute))
        {
            var delta = item.Type == MovementType.Entrada ? item.Quantity : -item.Quantity;
            var recordedAt = DateTime.Today.AddDays(item.Day).AddHours(item.Hour).AddMinutes(item.Minute);
            store.ApplyMovement(products[item.Product], item.Type, delta, item.Reason, SeedUser, recordedAt);
        }

        void AddRecipe(string name, string description, params (string Product, decimal Quantity)[] ingredients) =>
            store.AddRecipe(new Recipe
            {
                Name = name,
                Description = description,
                Ingredients = ingredients
                    .Select(item => new RecipeIngredient
                    {
                        ProductId = products[item.Product].Id,
                        QuantityPerServing = item.Quantity,
                        BaseUnit = products[item.Product].BaseUnit
                    })
                    .ToList()
            });

        AddRecipe("Margarita clásica", "Cóctel de tequila, naranja y limón.", ("Tequila blanco", 2), ("Jugo de naranja", 1), ("Limón", 1));
        AddRecipe("Michelada", "Cerveza preparada con limón.", ("Cerveza Pilsener", 1), ("Limón", 1));
        AddRecipe("Refresco de maracuyá", "Refresco natural de pulpa de maracuyá, servido en vaso con pajilla.", ("Pulpa de maracuyá", 2), ("Azúcar", 0.05m), ("Vasos acrílicos", 1), ("Pajillas", 1));
        AddRecipe("Limonada", "Limonada natural servida en vaso con pajilla.", ("Limón", 2), ("Azúcar", 0.04m), ("Vasos acrílicos", 1), ("Pajillas", 1));
    }
}
