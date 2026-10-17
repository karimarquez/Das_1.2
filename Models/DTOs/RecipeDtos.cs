namespace DAS.Models.DTOs;

public sealed record RecipeIngredientDto(int ProductId, string ProductName, decimal QuantityPerServing, string BaseUnit);

public sealed record RecipeDto(int Id, string Name, string Description, IReadOnlyList<RecipeIngredientDto> Ingredients);

public sealed record PrepareRecipeDto(int RecipeId, int Servings);

public sealed record IngredientDeductionDto(string ProductName, decimal Quantity, string Unit);

public sealed record RecipePreparationDto(string RecipeName, int Servings, IReadOnlyList<IngredientDeductionDto> Deductions);

public sealed record RecipeIngredientInputDto(int ProductId, decimal QuantityPerServing);

public sealed record SaveRecipeDto(string Name, string Description, IReadOnlyList<RecipeIngredientInputDto> Ingredients);
