using System.ComponentModel.DataAnnotations;

namespace DAS.Models.ViewModels;

public class RecipeConsumptionInputViewModel
{
    //Se enlaza desde la ruta /recetas/{recipeId}/preparar
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una receta válida.")]
    public int RecipeId { get; set; }

    [Display(Name = "Porciones")]
    [Required(ErrorMessage = "Indica el número de porciones.")]
    [Range(1, 1000, ErrorMessage = "Las porciones deben estar entre {1} y {2}.")]
    public int Servings { get; set; } = 1;
}
