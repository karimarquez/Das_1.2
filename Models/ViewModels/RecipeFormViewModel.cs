using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DAS.Models.ViewModels;

public class RecipeFormViewModel : IValidatableObject
{
    public const int MaxIngredients = 6;

    [Display(Name = "Nombre de la receta")]
    [Required(ErrorMessage = "Escribe el nombre de la receta.")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre {2} y {1} caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(160, ErrorMessage = "La descripción admite como máximo {1} caracteres.")]
    public string? Description { get; set; }

    //Se enlaza como colección: Ingredients[0].ProductId, Ingredients[0].QuantityPerServing...
    public List<RecipeIngredientInputViewModel> Ingredients { get; set; } = [];

    [BindNever, ValidateNever]
    public IEnumerable<SelectListItem> ProductOptions { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Ingredients.Any(ingredient => ingredient.ProductId.HasValue))
        {
            yield return new ValidationResult("Agrega al menos un ingrediente a la receta.");
        }

        for (var index = 0; index < Ingredients.Count; index++)
        {
            var ingredient = Ingredients[index];
            if (ingredient.ProductId.HasValue && !ingredient.QuantityPerServing.HasValue)
            {
                yield return new ValidationResult("Indica la cantidad por porción.", [$"{nameof(Ingredients)}[{index}].{nameof(RecipeIngredientInputViewModel.QuantityPerServing)}"]);
            }
            else if (!ingredient.ProductId.HasValue && ingredient.QuantityPerServing.HasValue)
            {
                yield return new ValidationResult("Selecciona el producto.", [$"{nameof(Ingredients)}[{index}].{nameof(RecipeIngredientInputViewModel.ProductId)}"]);
            }
        }

        if (Ingredients.Where(ingredient => ingredient.ProductId.HasValue).GroupBy(ingredient => ingredient.ProductId).Any(group => group.Count() > 1))
        {
            yield return new ValidationResult("Cada producto solo puede aparecer una vez en la receta.");
        }
    }
}

public class RecipeIngredientInputViewModel
{
    [Display(Name = "Producto")]
    public int? ProductId { get; set; }

    [Display(Name = "Cantidad por porción")]
    [Range(0.0001, 10000, ErrorMessage = "La cantidad por porción debe estar entre {1} y {2}.")]
    public decimal? QuantityPerServing { get; set; }
}
