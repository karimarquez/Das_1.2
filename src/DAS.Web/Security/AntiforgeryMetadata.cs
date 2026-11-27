using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DAS.Web.Security;

public static class AntiforgeryMetadata
{
    public static bool DeclaresPolicy(object metadata) =>
        metadata is IAntiforgeryPolicy or AutoValidateAntiforgeryTokenAttribute or ValidateAntiForgeryTokenAttribute;

    public static bool RequiresToken(IEnumerable<object> metadata) =>
        metadata.LastOrDefault(DeclaresPolicy) is AutoValidateAntiforgeryTokenAttribute or ValidateAntiForgeryTokenAttribute;
}
