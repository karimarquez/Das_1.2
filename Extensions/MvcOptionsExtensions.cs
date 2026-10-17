using Microsoft.AspNetCore.Mvc;

namespace DAS.Extensions;

public static class MvcOptionsExtensions
{
    public static MvcOptions UseSpanishValidation(this MvcOptions options)
    {
        //La obligatoriedad se declara con [Required] y mensajes en español en cada ViewModel
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

        var messages = options.ModelBindingMessageProvider;
        messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"El valor '{value}' no es válido para {field}.");
        messages.SetMissingBindRequiredValueAccessor(field => $"Falta un valor para {field}.");
        messages.SetMissingKeyOrValueAccessor(() => "Falta un valor obligatorio.");
        messages.SetMissingRequestBodyRequiredValueAccessor(() => "La solicitud no contiene datos.");
        messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"El valor '{value}' no es válido.");
        messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "El valor indicado no es válido.");
        messages.SetNonPropertyValueMustBeANumberAccessor(() => "El valor debe ser un número.");
        messages.SetUnknownValueIsInvalidAccessor(field => $"El valor indicado para {field} no es válido.");
        messages.SetValueIsInvalidAccessor(value => $"El valor '{value}' no es válido.");
        messages.SetValueMustBeANumberAccessor(field => $"{field} debe ser un número.");
        messages.SetValueMustNotBeNullAccessor(_ => "Este campo es obligatorio.");

        return options;
    }
}
