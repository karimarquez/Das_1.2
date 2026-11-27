using System.ComponentModel.DataAnnotations;
using DAS.Application.Validation;
using DAS.Domain.Catalogs;

namespace DAS.Application.DTOs;

public sealed record ProductDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Presentation { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string BaseUnit { get; init; } = string.Empty;
    public decimal BaseUnitsPerStockUnit { get; init; }
    public decimal Stock { get; init; }
    public decimal MinimumStock { get; init; }
    public decimal UnitCost { get; init; }
    public string Location { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsLowStock { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed record SaveProductDto : IValidatableObject
{
    [Display(Name = "Nombre del producto")]
    [Required(ErrorMessage = "Escribe el nombre del producto.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre {2} y {1} caracteres.")]
    [PlainText]
    public string Name { get; init; } = string.Empty;

    [Display(Name = "Categoría")]
    [Required(ErrorMessage = "Selecciona una categoría.")]
    public string Category { get; init; } = string.Empty;

    [Display(Name = "Presentación")]
    [Required(ErrorMessage = "Describe la presentación.")]
    [StringLength(50, ErrorMessage = "La presentación admite como máximo {1} caracteres.")]
    [PlainText]
    public string Presentation { get; init; } = string.Empty;

    [Display(Name = "Unidad de control")]
    [Required(ErrorMessage = "Selecciona la unidad de control.")]
    public string Unit { get; init; } = string.Empty;

    [Display(Name = "Unidad base de receta")]
    [Required(ErrorMessage = "Selecciona la unidad base de receta.")]
    public string BaseUnit { get; init; } = string.Empty;

    [Display(Name = "Unidades base por presentación")]
    [Range(0.0001, 1000000, ErrorMessage = "La equivalencia debe estar entre {1} y {2}.")]
    public decimal BaseUnitsPerStockUnit { get; init; } = 1;

    [Display(Name = "Stock mínimo")]
    [Range(0, 1000000, ErrorMessage = "El stock mínimo debe estar entre {1} y {2}.")]
    public decimal MinimumStock { get; init; }

    [Display(Name = "Costo unitario")]
    [Range(0, 100000, ErrorMessage = "El costo debe estar entre {1} y {2}.")]
    public decimal UnitCost { get; init; }

    [Display(Name = "Ubicación")]
    [Required(ErrorMessage = "Indica dónde se almacena el producto.")]
    [StringLength(40, ErrorMessage = "La ubicación admite como máximo {1} caracteres.")]
    [PlainText]
    public string Location { get; init; } = string.Empty;

    [Display(Name = "Existencia inicial")]
    [Range(0, 1000000, ErrorMessage = "La existencia inicial debe estar entre {1} y {2}.")]
    public decimal InitialStock { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsCatalogValue(CatalogKind.Category, Category))
        {
            yield return new ValidationResult("La categoría no pertenece al catálogo.", [nameof(Category)]);
        }

        if (!IsCatalogValue(CatalogKind.StockUnit, Unit))
        {
            yield return new ValidationResult("La unidad de control no pertenece al catálogo.", [nameof(Unit)]);
        }

        if (!IsCatalogValue(CatalogKind.BaseUnit, BaseUnit))
        {
            yield return new ValidationResult("La unidad base no pertenece al catálogo.", [nameof(BaseUnit)]);
        }

        if (string.Equals(Unit?.Trim(), BaseUnit?.Trim(), StringComparison.OrdinalIgnoreCase) && BaseUnitsPerStockUnit != 1)
        {
            yield return new ValidationResult(
                "Si la unidad de control y la unidad base son iguales, la equivalencia debe ser 1.",
                [nameof(BaseUnitsPerStockUnit)]);
        }
    }

    private static bool IsCatalogValue(CatalogKind kind, string? value) =>
        !string.IsNullOrWhiteSpace(value) && InventoryCatalog.For(kind).Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);
}

//Versión reducida para la API: no expone costos
public sealed record ProductStockDto(int Id, string Name, string Category, decimal Stock, string Unit, decimal MinimumStock, bool IsLowStock);
