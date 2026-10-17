using System.ComponentModel.DataAnnotations;
using DAS.Models.Catalogs;
using DAS.Models.Enums;
using DAS.Models.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DAS.Models.ViewModels;

public class ProductFormViewModel : IValidatableObject
{
    //El Id nunca se toma del formulario: en edición lo asigna el controlador desde la ruta
    [BindNever]
    public int Id { get; set; }

    [Display(Name = "Nombre del producto")]
    [Required(ErrorMessage = "Escribe el nombre del producto.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre {2} y {1} caracteres.")]
    [Remote("CheckName", "Products", AdditionalFields = nameof(Id), ErrorMessage = "Ya existe un producto con ese nombre.")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Categoría")]
    [Required(ErrorMessage = "Selecciona una categoría.")]
    [CatalogValue(CatalogKind.Category)]
    public string Category { get; set; } = InventoryCatalog.Categories[0];

    [Display(Name = "Presentación")]
    [Required(ErrorMessage = "Describe la presentación.")]
    [StringLength(50, ErrorMessage = "La presentación admite como máximo {1} caracteres.")]
    public string Presentation { get; set; } = string.Empty;

    [Display(Name = "Unidad de control")]
    [Required(ErrorMessage = "Selecciona la unidad de control.")]
    [CatalogValue(CatalogKind.StockUnit)]
    public string Unit { get; set; } = "unidades";

    [Display(Name = "Unidad base de receta")]
    [Required(ErrorMessage = "Selecciona la unidad base de receta.")]
    [CatalogValue(CatalogKind.BaseUnit)]
    public string BaseUnit { get; set; } = "unidades";

    [Display(Name = "Unidades base por presentación")]
    [Required(ErrorMessage = "Indica cuántas unidades base contiene la presentación.")]
    [Range(0.0001, 1000000, ErrorMessage = "La equivalencia debe estar entre {1} y {2}.")]
    public decimal BaseUnitsPerStockUnit { get; set; } = 1;

    [Display(Name = "Existencia inicial")]
    [Required(ErrorMessage = "Indica la existencia inicial.")]
    [Range(0, 1000000, ErrorMessage = "La existencia inicial debe estar entre {1} y {2}.")]
    public decimal InitialStock { get; set; }

    [Display(Name = "Stock mínimo")]
    [Required(ErrorMessage = "Indica el stock mínimo.")]
    [Range(0, 1000000, ErrorMessage = "El stock mínimo debe estar entre {1} y {2}.")]
    public decimal MinimumStock { get; set; }

    [Display(Name = "Costo unitario ($)")]
    [Required(ErrorMessage = "Indica el costo unitario.")]
    [Range(0, 100000, ErrorMessage = "El costo debe estar entre {1} y {2}.")]
    [DataType(DataType.Currency)]
    public decimal UnitCost { get; set; }

    [Display(Name = "Ubicación")]
    [Required(ErrorMessage = "Indica dónde se almacena el producto.")]
    [StringLength(40, ErrorMessage = "La ubicación admite como máximo {1} caracteres.")]
    public string Location { get; set; } = "Barra";

    [BindNever]
    public decimal CurrentStock { get; set; }

    public bool IsNew => Id == 0;

    [BindNever, ValidateNever]
    public IEnumerable<SelectListItem> CategoryOptions => ToOptions(InventoryCatalog.Categories);

    [BindNever, ValidateNever]
    public IEnumerable<SelectListItem> UnitOptions => ToOptions(InventoryCatalog.StockUnits);

    [BindNever, ValidateNever]
    public IEnumerable<SelectListItem> BaseUnitOptions => ToOptions(InventoryCatalog.BaseUnits);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.Equals(Unit, BaseUnit, StringComparison.OrdinalIgnoreCase) && BaseUnitsPerStockUnit != 1)
        {
            yield return new ValidationResult(
                "Si la unidad de control y la unidad base son iguales, la equivalencia debe ser 1.",
                [nameof(BaseUnitsPerStockUnit)]);
        }
    }

    private static IEnumerable<SelectListItem> ToOptions(IEnumerable<string> values) =>
        values.Select(value => new SelectListItem(value, value)).ToList();
}
