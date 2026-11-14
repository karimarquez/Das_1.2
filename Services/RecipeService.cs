using DAS.Configuration;
using DAS.Data;
using DAS.Mappings;
using DAS.Models.DTOs;
using DAS.Models.Entities;
using DAS.Models.Enums;
using DAS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DAS.Services;

public sealed class RecipeService(AppDbContext db, IOptions<InventoryOptions> options, ILogger<RecipeService> logger) : IRecipeService
{
    public IReadOnlyList<RecipeDto> GetAll()
    {
        var recipes = db.Recipes.AsNoTracking().Include(recipe => recipe.Ingredients).OrderBy(recipe => recipe.Id).ToList();
        var productIds = recipes.SelectMany(recipe => recipe.Ingredients).Select(ingredient => ingredient.ProductId).Distinct().ToList();
        var products = db.Products.AsNoTracking().Where(product => productIds.Contains(product.Id)).ToDictionary(product => product.Id);
        return recipes.Select(recipe => recipe.ToDto(products)).ToList();
    }

    public ServiceResult<RecipePreparationDto> Prepare(PrepareRecipeDto input)
    {
        if (input.Servings is < 1 or > 1000)
        {
            return ServiceResult<RecipePreparationDto>.Fail("Las porciones deben estar entre 1 y 1000.", nameof(PrepareRecipeDto.Servings));
        }

        var recipe = db.Recipes.Include(item => item.Ingredients).FirstOrDefault(item => item.Id == input.RecipeId);
        if (recipe is null)
        {
            return ServiceResult<RecipePreparationDto>.Fail("Selecciona una receta válida.");
        }

        var deductions = new List<(InventoryProduct Product, decimal Quantity)>();
        foreach (var ingredients in recipe.Ingredients.GroupBy(ingredient => ingredient.ProductId))
        {
            var product = db.Products.FirstOrDefault(item => item.Id == ingredients.Key);
            if (product is null || !product.IsActive)
            {
                return ServiceResult<RecipePreparationDto>.Fail($"{product?.Name ?? "Un ingrediente"} no está activo en el catálogo.");
            }

            if (product.BaseUnitsPerStockUnit <= 0 || ingredients.Any(ingredient => !string.Equals(ingredient.BaseUnit, product.BaseUnit, StringComparison.OrdinalIgnoreCase)))
            {
                return ServiceResult<RecipePreparationDto>.Fail($"No se pudo convertir la unidad de {product.Name}.");
            }

            var quantity = Math.Round(ingredients.Sum(ingredient => ingredient.QuantityPerServing) * input.Servings / product.BaseUnitsPerStockUnit, 4, MidpointRounding.AwayFromZero);
            if (quantity > product.Stock)
            {
                return ServiceResult<RecipePreparationDto>.Fail($"Existencia insuficiente de {product.Name}: disponible {product.Stock:0.##} {product.Unit}.");
            }

            if (quantity > 0)
            {
                deductions.Add((product, quantity));
            }
        }

        var now = DateTime.Now;
        var reason = $"Receta: {recipe.Name} · {input.Servings} porción(es)";
        foreach (var (product, quantity) in deductions)
        {
            db.AddMovement(product, MovementType.Consumo, -quantity, reason, options.Value.DefaultUserName, now);
        }
        db.SaveChanges();

        logger.LogInformation("Receta {RecipeName} preparada: {Servings} porción(es)", recipe.Name, input.Servings);
        return ServiceResult<RecipePreparationDto>.Ok(new RecipePreparationDto(
            recipe.Name,
            input.Servings,
            deductions.Select(item => new IngredientDeductionDto(item.Product.Name, item.Quantity, item.Product.Unit)).ToList()));
    }

    public ServiceResult<RecipeDto> Create(SaveRecipeDto input)
    {
        if (input.Ingredients.Count == 0)
        {
            return ServiceResult<RecipeDto>.Fail("Agrega al menos un ingrediente a la receta.");
        }

        if (input.Ingredients.GroupBy(ingredient => ingredient.ProductId).Any(group => group.Count() > 1))
        {
            return ServiceResult<RecipeDto>.Fail("Cada producto solo puede aparecer una vez en la receta.");
        }

        if (db.Recipes.Any(recipe => recipe.Name.ToLower() == input.Name.Trim().ToLower()))
        {
            return ServiceResult<RecipeDto>.Fail("Ya existe una receta con ese nombre.", nameof(SaveRecipeDto.Name));
        }

        var productIds = input.Ingredients.Select(item => item.ProductId).ToList();
        var products = db.Products.Where(product => productIds.Contains(product.Id) && product.IsActive).ToDictionary(product => product.Id);
        if (products.Count != productIds.Distinct().Count())
        {
            return ServiceResult<RecipeDto>.Fail("Uno de los ingredientes no existe o está inactivo.");
        }

        var ingredients = new List<RecipeIngredient>();
        foreach (var item in input.Ingredients)
        {
            var product = products[item.ProductId];
            if (item.QuantityPerServing <= 0)
            {
                return ServiceResult<RecipeDto>.Fail($"La cantidad de {product.Name} debe ser mayor que cero.");
            }

            ingredients.Add(new RecipeIngredient
            {
                ProductId = product.Id,
                QuantityPerServing = item.QuantityPerServing,
                BaseUnit = product.BaseUnit
            });
        }

        var recipe = new Recipe { Name = input.Name, Description = input.Description, Ingredients = ingredients };
        db.Recipes.Add(recipe);
        db.SaveChanges();

        logger.LogInformation("Receta {RecipeId} creada: {RecipeName}", recipe.Id, recipe.Name);
        return ServiceResult<RecipeDto>.Ok(recipe.ToDto(products));
    }
}
