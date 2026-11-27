using System.ComponentModel.DataAnnotations;

namespace DAS.Application.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class PlainTextAttribute : ValidationAttribute
{
    private static readonly char[] ForbiddenCharacters = ['<', '>'];

    public PlainTextAttribute() : base("{0} no puede contener etiquetas HTML ni los caracteres < o >.")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string text || text.IndexOfAny(ForbiddenCharacters) < 0;
}
