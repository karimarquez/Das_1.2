namespace DAS.Models.Entities;

public class Recipe
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<RecipeIngredient> Ingredients { get; set; } = [];
}

public class RecipeIngredient
{
    public int ProductId { get; set; }
    public decimal QuantityPerServing { get; set; }
    public string BaseUnit { get; set; } = string.Empty;
}
