using System.ComponentModel.DataAnnotations;

namespace DAS.Models.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotFutureDateAttribute : ValidationAttribute
{
    public NotFutureDateAttribute() : base("{0} no puede ser una fecha futura.")
    {
    }

    public override bool IsValid(object? value) => value is not DateTime date || date.Date <= DateTime.Today;
}
