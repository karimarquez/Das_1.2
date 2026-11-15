using DAS.Application.Abstractions;
using DAS.Application.Abstractions.Persistence;
using DAS.Application.Common;
using DAS.Application.DTOs;
using DAS.Application.Mappings;
using DAS.Application.Services.Interfaces;
using DAS.Domain.Entities;
using DAS.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DAS.Application.Services;

public sealed class RecipeService(
    IRecipeRepository recipes,
    IProductRepository products,
    IMovementRepository movements,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<RecipeService> logger) : IRecipeService
{
    public async Task<IReadOnlyList<RecipeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var all = await recipes.ListWithIngredientsAsync(cancellationToken);
        var productIds = all.SelectMany(recipe => recipe.Ingredients).Select(ingredient => ingredient.ProductId).Distinct().ToList();
        var names = (await products.GetByIdsAsync(productIds, cancellationToken)).ToDictionary(product => product.Id);
        return all.Select(recipe => recipe.ToDto(names)).ToList();
    }

    public async Task<RecipeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var recipe = await recipes.GetWithIngredientsAsync(id, cancellationToken);
        if (recipe is null)
        {
            return null;
        }

        var productIds = recipe.Ingredients.Select(ingredient => ingredient.ProductId).Distinct().ToList();
        var names = (await products.GetByIdsAsync(productIds, cancellationToken)).ToDictionary(product => product.Id);
        return recipe.ToDto(names);
    }

    public async Task<ServiceResult<RecipePreparationDto>> PrepareAsync(PrepareRecipeDto input, CancellationToken cancellationToken = default)
    {
        if (input.Servings is < 1 or > 1000)
        {
            return ServiceResult<RecipePreparationDto>.Fail("Las porciones deben estar entre 1 y 1000.", nameof(PrepareRecipeDto.Servings));
        }

        var result = await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var recipe = await recipes.GetWithIngredientsAsync(input.RecipeId, token);
            if (recipe is null)
            {
                return ServiceResult<RecipePreparationDto>.Fail("Selecciona una receta válida.");
            }

            var productIds = recipe.Ingredients.Select(ingredient => ingredient.ProductId).Distinct().ToList();
            var available = (await products.GetByIdsAsync(productIds, token)).ToDictionary(product => product.Id);

            //Primero se validan todos los ingredientes; si uno falla no se descuenta ninguno
            var deductions = new List<(InventoryProduct Product, decimal Quantity)>();
            foreach (var ingredients in recipe.Ingredients.GroupBy(ingredient => ingredient.ProductId))
            {
                available.TryGetValue(ingredients.Key, out var product);
                if (product is null || !product.IsActive)
                {
                    return ServiceResult<RecipePreparationDto>.Fail($"{product?.Name ?? "Un ingrediente"} no está activo en el catálogo.");
                }

                if (product.BaseUnitsPerStockUnit <= 0
                    || ingredients.Any(ingredient => !string.Equals(ingredient.BaseUnit, product.BaseUnit, StringComparison.OrdinalIgnoreCase)))
                {
                    return ServiceResult<RecipePreparationDto>.Fail($"No se pudo convertir la unidad de {product.Name}.");
                }

                var quantity = product.ToStockUnits(ingredients.Sum(ingredient => ingredient.QuantityPerServing) * input.Servings);
                if (!product.HasStockFor(quantity))
                {
                    return ServiceResult<RecipePreparationDto>.Fail($"Existencia insuficiente de {product.Name}: disponible {product.Stock:0.##} {product.Unit}.");
                }

                if (quantity > 0)
                {
                    deductions.Add((product, quantity));
                }
            }

            var now = clock.GetLocalNow().DateTime;
            var reason = $"Receta: {recipe.Name} · {input.Servings} porción(es)";
            foreach (var (product, quantity) in deductions)
            {
                movements.Add(product.RegisterMovement(MovementType.Consumo, quantity, reason, currentUser.UserName, now));
            }

            await unitOfWork.SaveChangesAsync(token);
            return ServiceResult<RecipePreparationDto>.Ok(new RecipePreparationDto(
                recipe.Name,
                input.Servings,
                deductions.Select(item => new IngredientDeductionDto(item.Product.Name, item.Quantity, item.Product.Unit)).ToList()));
        }, cancellationToken);

        if (result.Succeeded)
        {
            logger.LogInformation("Receta {RecipeName} preparada: {Servings} porción(es)", result.Value!.RecipeName, input.Servings);
        }

        return result;
    }

    public async Task<ServiceResult<RecipeDto>> CreateAsync(SaveRecipeDto input, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateInputAsync(input, null, cancellationToken);
        if (!validation.Succeeded)
        {
            return ServiceResult<RecipeDto>.Fail(validation.Error!, validation.Field);
        }

        var catalog = validation.Value!;
        var ingredients = input.Ingredients.Select(item =>
        {
            var product = catalog[item.ProductId];
            return new RecipeIngredient
            {
                ProductId = product.Id,
                QuantityPerServing = item.QuantityPerServing,
                BaseUnit = product.BaseUnit
            };
        }).ToList();

        var recipe = new Recipe { Name = input.Name, Description = input.Description, Ingredients = ingredients };
        recipes.Add(recipe);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Receta {RecipeId} creada: {RecipeName}", recipe.Id, recipe.Name);
        return ServiceResult<RecipeDto>.Ok(recipe.ToDto(catalog));
    }

    public async Task<ServiceResult<RecipeDto>> UpdateAsync(int id, SaveRecipeDto input, CancellationToken cancellationToken = default)
    {
        var recipe = await recipes.GetTrackedWithIngredientsAsync(id, cancellationToken);
        if (recipe is null)
        {
            return ServiceResult<RecipeDto>.Fail("La receta no existe.");
        }

        var validation = await ValidateInputAsync(input, id, cancellationToken);
        if (!validation.Succeeded)
        {
            return ServiceResult<RecipeDto>.Fail(validation.Error!, validation.Field);
        }

        var catalog = validation.Value!;
        recipe.Name = input.Name;
        recipe.Description = input.Description;
        recipe.Ingredients.Clear();
        recipe.Ingredients.AddRange(input.Ingredients.Select(item =>
        {
            var product = catalog[item.ProductId];
            return new RecipeIngredient
            {
                ProductId = product.Id,
                QuantityPerServing = item.QuantityPerServing,
                BaseUnit = product.BaseUnit
            };
        }));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Receta {RecipeId} actualizada", recipe.Id);
        return ServiceResult<RecipeDto>.Ok(recipe.ToDto(catalog));
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var recipe = await recipes.GetTrackedWithIngredientsAsync(id, cancellationToken);
        if (recipe is null)
        {
            return ServiceResult.Fail("La receta no existe.");
        }

        recipes.Remove(recipe);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Receta {RecipeId} eliminada", recipe.Id);
        return ServiceResult.Ok();
    }

    private async Task<ServiceResult<Dictionary<int, InventoryProduct>>> ValidateInputAsync(
        SaveRecipeDto input,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        if (input.Ingredients.Count == 0)
        {
            return ServiceResult<Dictionary<int, InventoryProduct>>.Fail("Agrega al menos un ingrediente a la receta.");
        }

        if (input.Ingredients.GroupBy(ingredient => ingredient.ProductId).Any(group => group.Count() > 1))
        {
            return ServiceResult<Dictionary<int, InventoryProduct>>.Fail("Cada producto solo puede aparecer una vez en la receta.");
        }

        if (await recipes.NameExistsAsync(input.Name, excludeId, cancellationToken))
        {
            return ServiceResult<Dictionary<int, InventoryProduct>>.Fail("Ya existe una receta con ese nombre.", nameof(SaveRecipeDto.Name));
        }

        var productIds = input.Ingredients.Select(item => item.ProductId).ToList();
        var catalog = (await products.GetByIdsAsync(productIds, cancellationToken))
            .Where(product => product.IsActive)
            .ToDictionary(product => product.Id);
        if (catalog.Count != productIds.Count)
        {
            return ServiceResult<Dictionary<int, InventoryProduct>>.Fail("Uno de los ingredientes no existe o está inactivo.");
        }

        var invalidIngredient = input.Ingredients.FirstOrDefault(item => item.QuantityPerServing <= 0);
        if (invalidIngredient is not null)
        {
            return ServiceResult<Dictionary<int, InventoryProduct>>.Fail(
                $"La cantidad de {catalog[invalidIngredient.ProductId].Name} debe ser mayor que cero.");
        }

        return ServiceResult<Dictionary<int, InventoryProduct>>.Ok(catalog);
    }
}
