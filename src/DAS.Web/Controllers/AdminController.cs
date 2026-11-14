using DAS.Application.Security;
using DAS.Infrastructure.Identity;
using DAS.Web.Extensions;
using DAS.Web.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DAS.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[Route("admin/usuarios")]
public sealed class AdminController(
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Users()
    {
        var users = userManager.Users.OrderBy(user => user.Email).ToList();
        var model = new List<UserRoleViewModel>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            model.Add(new UserRoleViewModel(user.Id, user.Email ?? user.UserName ?? "", roles.FirstOrDefault() ?? AppRoles.Consulta));
        }

        return View(model);
    }

    [HttpPost("rol")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string userId, string role)
    {
        if (!AppRoles.All.Contains(role, StringComparer.Ordinal))
            return BadRequest();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return NotFound();

        var currentRoles = await userManager.GetRolesAsync(user);
        if (user.Id == userManager.GetUserId(User) && currentRoles.Contains(AppRoles.Admin) && role != AppRoles.Admin)
        {
            TempData.Error("No puedes quitarte el rol de administrador desde tu propia sesión.");
            return RedirectToAction(nameof(Users));
        }

        if (currentRoles.Contains(AppRoles.Admin) && role != AppRoles.Admin
            && (await userManager.GetUsersInRoleAsync(AppRoles.Admin)).Count <= 1)
        {
            TempData.Error("Debe permanecer al menos un administrador en el sistema.");
            return RedirectToAction(nameof(Users));
        }

        var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            TempData.Error("No fue posible actualizar los permisos de la cuenta.");
            return RedirectToAction(nameof(Users));
        }

        var addResult = await userManager.AddToRoleAsync(user, role);
        if (!addResult.Succeeded)
        {
            TempData.Error("No fue posible asignar el rol seleccionado.");
            return RedirectToAction(nameof(Users));
        }

        TempData.Success($"Permisos actualizados para {user.Email}.");
        return RedirectToAction(nameof(Users));
    }
}
