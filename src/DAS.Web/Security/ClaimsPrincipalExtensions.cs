using System.Security.Claims;
using DAS.Application.Security;

namespace DAS.Web.Security;

public static class ClaimsPrincipalExtensions
{
    //Misma regla que [Authorize(Roles = AppRoles.Operadores)], para mostrar u ocultar acciones en las vistas
    public static bool CanManageInventory(this ClaimsPrincipal user) =>
        user.IsInRole(AppRoles.Admin) || user.IsInRole(AppRoles.Encargado);

    public static string RoleLabel(this ClaimsPrincipal user) =>
        user.IsInRole(AppRoles.Admin) ? "ADMIN" : user.IsInRole(AppRoles.Encargado) ? "ENCARGADO" : "CONSULTA";
}
