using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace DAS.Web.Security;

public sealed class ViewControllersAntiforgeryConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (!typeof(Controller).IsAssignableFrom(controller.ControllerType)
            || controller.Filters.Any(AntiforgeryMetadata.DeclaresPolicy))
        {
            return;
        }

        controller.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    }
}
