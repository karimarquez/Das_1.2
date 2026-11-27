using System.ComponentModel.DataAnnotations;
using DAS.Application.Validation;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;

namespace DAS.Web.Models.Validation;

public sealed class LocalValidationAttributeAdapterProvider : IValidationAttributeAdapterProvider
{
    private readonly ValidationAttributeAdapterProvider _defaults = new();

    public IAttributeAdapter? GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer? stringLocalizer) =>
        attribute is PlainTextAttribute plainText
            ? new PlainTextAttributeAdapter(plainText, stringLocalizer)
            : _defaults.GetAttributeAdapter(attribute, stringLocalizer);
}
