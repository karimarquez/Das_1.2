using System.ComponentModel.DataAnnotations;
using DAS.Models.Enums;
using DAS.Models.Validation;

namespace DAS.Models.ViewModels;

public class MovementInputViewModel : IValidatableObject
{
    [Display(Name = "Producto")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un producto.")]
    public int ProductId { get; set; }

    [Display(Name = "Tipo de movimiento")]
    [Required(ErrorMessage = "Selecciona el tipo de movimiento.")]
    [EnumDataType(typeof(MovementType), ErrorMessage = "El tipo de movimiento no es válido.")]
    public MovementType Type { get; set; } = MovementType.Entrada;

    [Display(Name = "Cantidad")]
    [Required(ErrorMessage = "Indica la cantidad.")]
    [Range(0.01, 1000000, ErrorMessage = "La cantidad debe estar entre {1} y {2}.")]
    public decimal Quantity { get; set; }

    [Display(Name = "Motivo")]
    [Required(ErrorMessage = "Indica el motivo del movimiento.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "El motivo debe tener entre {2} y {1} caracteres.")]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Fecha del movimiento")]
    [DataType(DataType.Date)]
    [NotFutureDate]
    public DateTime? Date { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type == MovementType.Ajuste)
        {
            yield return new ValidationResult("Los ajustes solo se generan desde el conteo físico.", [nameof(Type)]);
        }
    }
}
