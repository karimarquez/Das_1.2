using System.ComponentModel.DataAnnotations;
using DAS.Models.Catalogs;
using DAS.Models.Enums;

namespace DAS.Models.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class CatalogValueAttribute : ValidationAttribute
{
    public CatalogValueAttribute(CatalogKind kind) : base("El valor seleccionado en {0} no pertenece al catálogo.")
    {
        Kind = kind;
    }

    public CatalogKind Kind { get; }

    public override bool IsValid(object? value) =>
        value is not string text
        || string.IsNullOrWhiteSpace(text)
        || InventoryCatalog.For(Kind).Contains(text.Trim(), StringComparer.OrdinalIgnoreCase);
}
