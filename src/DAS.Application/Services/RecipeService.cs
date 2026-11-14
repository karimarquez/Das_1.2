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
        if (input.Ingredients.Count == 0)
        {
            return ServiceResult<RecipeDto>.Fail("Agrega al menos un ingrediente a la receta.");
        }

        if (input.Ingredients.GroupBy(ingredient => ingredient.ProductId).Any(group => group.Count() > 1))
        {
            return ServiceResult<RecipeDto>.Fail("Cada producto solo puede aparecer una vez en la receta.");
        }

        if (await recipes.NameExistsAsync(input.Name, cancellationToken))
        {
            return ServiceResult<RecipeDto>.Fail("Ya existe una receta con ese nombre.", nameof(SaveRecipeDto.Name));
        }

        var productIds = input.Ingredients.Select(item => item.ProductId).ToList();
        var catalog = (await products.GetByIdsAsync(productIds, cancellationToken))
            .Where(product => product.IsActive)
            .ToDictionary(product => product.Id);
        if (catalog.Count != productIds.Count)
        {
            return ServiceResult<RecipeDto>.Fail("Uno de los ingredientes no existe o está inactivo.");
        }

        var ingredients = new List<RecipeIngredient>();
        foreach (var item in input.Ingredients)
        {
            var product = catalog[item.ProductId];
            if (item.QuantityPerServing <= 0)
            {
                return ServiceResult<RecipeDto>.Fail($"La cantidad de {product.Name} debe ser mayor que cero.");
            }

            //La receta guarda la unidad base vigente del producto para validar la conversión al preparar
            ingredients.Add(new RecipeIngredient
            {
                ProductId = product.Id,
                QuantityPerServing = item.QuantityPerServing,
                BaseUnit = product.BaseUnit
            });
        }

        var recipe = new Recipe { Name = input.Name, Description = input.Description, Ingredients = ingredients };
        recipes.Add(recipe);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Receta {RecipeId} creada: {RecipeName}", recipe.Id, recipe.Name);
        return ServiceResult<RecipeDto>.Ok(recipe.ToDto(catalog));
    }
}
