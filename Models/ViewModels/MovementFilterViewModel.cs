using System.ComponentModel.DataAnnotations;
using DAS.Models.Enums;

namespace DAS.Models.ViewModels;

//Se llena desde la query string: /movimientos?Filter.Type=3&Filter.From=2026-10-01
public class MovementFilterViewModel : IValidatableObject
{
    [Display(Name = "Producto")]
    public int? ProductId { get; set; }

    [Display(Name = "Tipo")]
    [EnumDataType(typeof(MovementType), ErrorMessage = "El tipo de movimiento no es válido.")]
    public MovementType? Type { get; set; }

    [Display(Name = "Desde")]
    [DataType(DataType.Date)]
    public DateTime? From { get; set; }

    [Display(Name = "Hasta")]
    [DataType(DataType.Date)]
    public DateTime? To { get; set; }

    public bool HasFilters => ProductId.HasValue || Type.HasValue || From.HasValue || To.HasValue;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue && To.HasValue && From.Value.Date > To.Value.Date)
        {
            yield return new ValidationResult("La fecha inicial no puede ser mayor que la fecha final.", [nameof(To)]);
        }
    }
}
