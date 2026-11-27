using DAS.Application.Validation;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;

namespace DAS.Web.Models.Validation;

public sealed class PlainTextAttributeAdapter(PlainTextAttribute attribute, IStringLocalizer? stringLocalizer)
    : AttributeAdapterBase<PlainTextAttribute>(attribute, stringLocalizer)
{
    public override void AddValidation(ClientModelValidationContext context)
    {
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-plaintext", GetErrorMessage(context));
    }

    public override string GetErrorMessage(ModelValidationContextBase validationContext) =>
        GetErrorMessage(validationContext.ModelMetadata, validationContext.ModelMetadata.GetDisplayName());
}
