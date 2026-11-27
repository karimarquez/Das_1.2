using System.ComponentModel.DataAnnotations;
using DAS.Application.Common;

namespace DAS.Application.Validation;

public static class InputValidator
{
    public static ServiceResult Validate(object input)
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true))
        {
            return ServiceResult.Ok();
        }

        var first = results[0];
        return ServiceResult.Fail(first.ErrorMessage ?? "Los datos enviados no son válidos.", first.MemberNames.FirstOrDefault());
    }
}
