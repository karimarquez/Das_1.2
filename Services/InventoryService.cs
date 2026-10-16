using DAS.Models;

namespace DAS.Services;

public class InventoryService
{
    private readonly object _sync = new();
    private readonly List<InventoryProduct> _products =
    [
        new() { Id = 1, Name = "Cerveza Pilsener", Category = "Cervezas y sodas", Presentation = "Botella 330 ml", Unit = "botellas", BaseUnit = "botellas", BaseUnitsPerStockUnit = 1, Stock = 36, MinimumStock = 24, UnitCost = 0.85m, Location = "Refrigerador 01" },
        new() { Id = 2, Name = "Agua mineral", Category = "Cervezas y sodas", Presentation = "Botella 600 ml", Unit = "botellas", BaseUnit = "botellas", BaseUnitsPerStockUnit = 1, Stock = 18, MinimumStock = 20, UnitCost = 0.55m, Location = "Refrigerador 01" },
        new() { Id = 3, Name = "Tequila blanco", Category = "Licores", Presentation = "Botella 750 ml", Unit = "botellas", BaseUnit = "oz", BaseUnitsPerStockUnit = 25.36m, Stock = 7.5m, MinimumStock = 4, UnitCost = 18.50m, Location = "Estante de licores" },
        new() { Id = 4, Name = "Jugo de naranja", Category = "Zumos y jugos", Presentation = "Galón", Unit = "galones", BaseUnit = "oz", BaseUnitsPerStockUnit = 128, Stock = 3, MinimumStock = 2, UnitCost = 4.25m, Location = "Refrigerador 02" },
        new() { Id = 5, Name = "Leche evaporada", Category = "Lácteos", Presentation = "Lata 360 ml", Unit = "latas", BaseUnit = "oz", BaseUnitsPerStockUnit = 12.17m, Stock = 9, MinimumStock = 6, UnitCost = 1.15m, Location = "Refrigerador 02" },
        new() { Id = 6, Name = "Limón", Category = "Frutas e insumos", Presentation = "Unidad", Unit = "unidades", BaseUnit = "unidades", BaseUnitsPerStockUnit = 1, Stock = 14, MinimumStock = 18, UnitCost = 0.08m, Location = "Barra fría" },
        new() { Id = 7, Name = "Vasos acrílicos", Category = "Desechables", Presentation = "Paquete de 25", Unit = "paquetes", BaseUnit = "unidades", BaseUnitsPerStockUnit = 25, Stock = 5, MinimumStock = 3, UnitCost = 2.40m, Location = "Bodega de bar" }
    ];
    private readonly List<Recipe> _recipes =
    [
        new() { Id = 1, Name = "Margarita clásica", Description = "Cóctel de tequila, naranja y limón.", Ingredients = [new() { ProductId = 3, ProductName = "Tequila blanco", QuantityPerServing = 2, BaseUnit = "oz" }, new() { ProductId = 4, ProductName = "Jugo de naranja", QuantityPerServing = 1, BaseUnit = "oz" }, new() { ProductId = 6, ProductName = "Limón", QuantityPerServing = 1, BaseUnit = "unidades" }] },
        new() { Id = 2, Name = "Michelada", Description = "Cerveza preparada con limón.", Ingredients = [new() { ProductId = 1, ProductName = "Cerveza Pilsener", QuantityPerServing = 1, BaseUnit = "botellas" }, new() { ProductId = 6, ProductName = "Limón", QuantityPerServing = 1, BaseUnit = "unidades" }] }
    ];
    private readonly List<InventoryMovement> _movements =
    [
        new() { Id = 1, ProductId = 1, ProductName = "Cerveza Pilsener", Type = "Entrada", Quantity = 24, Unit = "botellas", Reason = "Reposición de proveedor", RecordedAt = DateTime.Today.AddHours(9).AddMinutes(12) },
        new() { Id = 2, ProductId = 3, ProductName = "Tequila blanco", Type = "Consumo", Quantity = 0.5m, Unit = "botellas", Reason = "Cierre de turno", RecordedAt = DateTime.Today.AddHours(11).AddMinutes(38) },
        new() { Id = 3, ProductId = 6, ProductName = "Limón", Type = "Merma", Quantity = 3, Unit = "unidades", Reason = "Producto dañado", RecordedAt = DateTime.Today.AddHours(12).AddMinutes(5) },
        new() { Id = 4, ProductId = 4, ProductName = "Jugo de naranja", Type = "Consumo", Quantity = 1, Unit = "galones", Reason = "Preparación de bebidas", RecordedAt = DateTime.Today.AddDays(-1).AddHours(17) }
    ];
    private int _nextProductId = 8;
    private int _nextMovementId = 5;

    public InventoryDashboardViewModel GetDashboard()
    {
        lock (_sync)
        {
            var todayMovements = _movements.Where(movement => movement.RecordedAt.Date == DateTime.Today).ToList();
            return new InventoryDashboardViewModel
            {
                Products = _products.Where(product => product.IsActive).OrderBy(product => product.Name).ToList(),
                RecentMovements = _movements.OrderByDescending(movement => movement.RecordedAt).Take(5).ToList(),
                ProductCount = _products.Count(product => product.IsActive),
                LowStockCount = _products.Count(product => product.IsActive && product.Stock <= product.MinimumStock),
                TodayMovementCount = todayMovements.Count,
                TodayConsumptionCount = todayMovements.Count(movement => movement.Type == "Consumo")
            };
        }
    }

    public IReadOnlyList<InventoryProduct> GetProducts()
    {
        lock (_sync)
        {
            return _products.Where(product => product.IsActive).OrderBy(product => product.Name).ToList();
        }
    }

    public IReadOnlyList<Recipe> GetRecipes()
    {
        lock (_sync)
        {
            return _recipes.ToList();
        }
    }

    public bool ConsumeRecipe(RecipeConsumptionInputViewModel input, out string error)
    {
        lock (_sync)
        {
            var recipe = _recipes.FirstOrDefault(item => item.Id == input.RecipeId);
            if (recipe is null)
            {
                error = "Selecciona una receta válida.";
                return false;
            }

            var deductions = new List<(InventoryProduct Product, decimal StockQuantity)>();
            foreach (var ingredient in recipe.Ingredients)
            {
                var product = _products.FirstOrDefault(item => item.Id == ingredient.ProductId && item.IsActive);
                if (product is null || product.BaseUnit != ingredient.BaseUnit || product.BaseUnitsPerStockUnit <= 0)
                {
                    error = $"No se pudo convertir la unidad de {ingredient.ProductName}.";
                    return false;
                }

                var stockQuantity = ingredient.QuantityPerServing * input.Servings / product.BaseUnitsPerStockUnit;
                if (stockQuantity > product.Stock)
                {
                    error = $"Existencia insuficiente de {product.Name}: disponible {product.Stock:0.##} {product.Unit}.";
                    return false;
                }

                deductions.Add((product, stockQuantity));
            }

            foreach (var deduction in deductions)
            {
                deduction.Product.Stock -= deduction.StockQuantity;
                AddMovement(deduction.Product, "Consumo", deduction.StockQuantity, $"Receta: {recipe.Name} · {input.Servings} porción(es)");
            }

            error = string.Empty;
            return true;
        }
    }

    public IReadOnlyList<InventoryMovement> GetMovements(DateTime? from = null, DateTime? to = null)
    {
        lock (_sync)
        {
            return _movements
                .Where(movement => (!from.HasValue || movement.RecordedAt.Date >= from.Value.Date)
                    && (!to.HasValue || movement.RecordedAt.Date <= to.Value.Date))
                .OrderByDescending(movement => movement.RecordedAt)
                .ToList();
        }
    }

    public bool RecordMovement(MovementInputViewModel input, out string error)
    {
        lock (_sync)
        {
            var product = _products.FirstOrDefault(item => item.Id == input.ProductId && item.IsActive);
            if (product is null)
            {
                error = "Selecciona un producto válido.";
                return false;
            }

            if (input.Type is not ("Entrada" or "Consumo" or "Merma"))
            {
                error = "El tipo de movimiento no es válido.";
                return false;
            }

            if (input.Type != "Entrada" && input.Quantity > product.Stock)
            {
                error = $"La existencia disponible es {product.Stock:0.##} {product.Unit}.";
                return false;
            }

            product.Stock += input.Type == "Entrada" ? input.Quantity : -input.Quantity;
            AddMovement(product, input.Type, input.Quantity, input.Reason);
            error = string.Empty;
            return true;
        }
    }

    public bool RegisterCount(PhysicalCountInputViewModel input)
    {
        lock (_sync)
        {
            var product = _products.FirstOrDefault(item => item.Id == input.ProductId && item.IsActive);
            if (product is null)
            {
                return false;
            }

            var difference = input.ActualStock - product.Stock;
            if (difference != 0)
            {
                product.Stock = input.ActualStock;
                AddMovement(product, "Ajuste", Math.Abs(difference), difference > 0 ? "Ajuste por conteo físico · sobrante" : "Ajuste por conteo físico · faltante");
            }

            return true;
        }
    }

    public void AddProduct(ProductInputViewModel input)
    {
        lock (_sync)
        {
            _products.Add(new InventoryProduct
            {
                Id = _nextProductId++,
                Name = input.Name.Trim(),
                Category = input.Category,
                Presentation = input.Presentation.Trim(),
                Unit = input.Unit.Trim(),
                BaseUnit = input.BaseUnit.Trim(),
                BaseUnitsPerStockUnit = input.BaseUnitsPerStockUnit,
                Stock = input.Stock,
                MinimumStock = input.MinimumStock,
                UnitCost = input.UnitCost
            });
        }
    }

    private void AddMovement(InventoryProduct product, string type, decimal quantity, string reason)
    {
        _movements.Add(new InventoryMovement
        {
            Id = _nextMovementId++,
            ProductId = product.Id,
            ProductName = product.Name,
            Type = type,
            Quantity = quantity,
            Unit = product.Unit,
            Reason = reason.Trim(),
            RecordedAt = DateTime.Now
        });
    }
}