using DAS.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DAS.Controllers;

[AllowAnonymous]
[Route("cuenta")]
public sealed class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : Controller
{
    [HttpGet("ingresar")]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost("ingresar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel input)
    {
        if (!ModelState.IsValid)
            return View(input);

        var result = await signInManager.PasswordSignInAsync(
            input.Email, input.Password, input.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(input.ReturnUrl) && Url.IsLocalUrl(input.ReturnUrl))
                return LocalRedirect(input.ReturnUrl);
            return RedirectToAction(nameof(InventoryController.Index), "Inventory");
        }

        ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
        return View(input);
    }

    [HttpGet("registro")]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpGet("acceso-denegado")]
    public IActionResult AccessDenied() => View();

    [HttpPost("registro")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel input)
    {
        if (!ModelState.IsValid)
            return View(input);

        var user = new ApplicationUser { UserName = input.Email.Trim(), Email = input.Email.Trim() };
        var result = await userManager.CreateAsync(user, input.Password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Consulta");
            await signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction(nameof(InventoryController.Index), "Inventory");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
        return View(input);
    }

    [HttpPost("salir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
}
