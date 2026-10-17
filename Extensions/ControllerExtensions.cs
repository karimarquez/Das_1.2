using DAS.Models.DTOs;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DAS.Extensions;

public static class ControllerExtensions
{
    public const string SuccessKey = "Success";
    public const string ErrorKey = "Error";

    //Mensajes que sobreviven a una redirección (patrón Post-Redirect-Get)
    public static void Success(this ITempDataDictionary tempData, string message) => tempData[SuccessKey] = message;

    public static void Error(this ITempDataDictionary tempData, string message) => tempData[ErrorKey] = message;

    public static void AddServiceError(this ModelStateDictionary modelState, ServiceResult result) =>
        modelState.AddModelError(result.Field ?? string.Empty, result.Error ?? "No fue posible completar la operación.");

    public static string FirstError(this ModelStateDictionary modelState, string fallback) =>
        modelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message)) ?? fallback;
}
