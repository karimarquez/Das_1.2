using DAS.Domain.Common;

namespace DAS.Domain.Entities;

public class Recipe : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<RecipeIngredient> Ingredients { get; set; } = [];
}

//Cantidad por porción expresada en la unidad base del producto (oz, lb, unidades)
public class RecipeIngredient
{
    public int ProductId { get; set; }
    public decimal QuantityPerServing { get; set; }
    public string BaseUnit { get; set; } = string.Empty;
}
