using System.Diagnostics;
using DAS.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DAS.Controllers;

public class HomeController : Controller
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    //Páginas de estado: UseStatusCodePagesWithReExecute("/error/{0}") vuelve a ejecutar esta acción
    [Route("error/{code:int}")]
    public IActionResult Status(int code)
    {
        var model = code == StatusCodes.Status404NotFound
            ? new StatusViewModel(code, "No encontramos lo que buscas", "El producto, la receta o la página solicitada no existe o cambió de dirección.")
            : new StatusViewModel(code, "No fue posible completar la solicitud", "Revisa los datos e inténtalo de nuevo.");

        return View(model);
    }
}
