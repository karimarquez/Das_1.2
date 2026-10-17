using System.ComponentModel.DataAnnotations;
using DAS.Models.DTOs;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DAS.Models.ViewModels;

//Hoja de conteo de cierre: se enlaza como colección Lines[0].CountedStock, Lines[1].CountedStock...
public class PhysicalCountSheetViewModel : IValidatableObject
{
    public List<PhysicalCountLineViewModel> Lines { get; set; } = [];

    [BindNever, ValidateNever]
    public IReadOnlyList<PhysicalCountDto> RecentCounts { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Lines.Any(line => line.CountedStock.HasValue))
        {
            yield return new ValidationResult("Ingresa al menos una cantidad contada.");
        }
    }
}

public class PhysicalCountLineViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "El producto del conteo no es válido.")]
    public int ProductId { get; set; }

    [Display(Name = "Cantidad contada")]
    [Range(0, 1000000, ErrorMessage = "La cantidad contada debe estar entre {1} y {2}.")]
    public decimal? CountedStock { get; set; }

    [BindNever]
    public string ProductName { get; set; } = string.Empty;

    [BindNever]
    public string Presentation { get; set; } = string.Empty;

    [BindNever]
    public string Category { get; set; } = string.Empty;

    [BindNever]
    public string Location { get; set; } = string.Empty;

    [BindNever]
    public string Unit { get; set; } = string.Empty;
}
