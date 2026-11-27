using System.Diagnostics;
using DAS.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DAS.Web.Controllers;

[IgnoreAntiforgeryToken]
public class HomeController : Controller
{
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    //Páginas de estado: UseStatusCodePagesWithReExecute("/error/{0}") vuelve a ejecutar esta acción
    [AllowAnonymous]
    [Route("error/{code:int}")]
    public IActionResult Status(int code)
    {
        var model = code switch
        {
            StatusCodes.Status404NotFound => new StatusViewModel(code, "No encontramos lo que buscas", "El producto, la receta o la página solicitada no existe o cambió de dirección."),
            StatusCodes.Status429TooManyRequests => new StatusViewModel(code, "Demasiados intentos", "Por seguridad se limitaron los intentos de acceso desde este equipo. Espera un minuto e inténtalo de nuevo."),
            _ => new StatusViewModel(code, "No fue posible completar la solicitud", "Revisa los datos e inténtalo de nuevo.")
        };

        return View(model);
    }
}
