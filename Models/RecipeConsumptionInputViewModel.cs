using System.ComponentModel.DataAnnotations;

namespace DAS.Models;

public class RecipeConsumptionInputViewModel
{
    [Required]
    public int RecipeId { get; set; }

    [Range(1, 1000)]
    public int Servings { get; set; } = 1;
}