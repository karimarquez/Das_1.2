using DAS.Application.Security;
using DAS.Infrastructure.Identity;
using DAS.Web.Models.Auth;
using DAS.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DAS.Web.Controllers;

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
    [EnableRateLimiting(SecurityPolicies.CredentialsRateLimit)]
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
    [EnableRateLimiting(SecurityPolicies.CredentialsRateLimit)]
    public async Task<IActionResult> Register(RegisterViewModel input)
    {
        if (!ModelState.IsValid)
            return View(input);

        var user = new ApplicationUser { UserName = input.Email.Trim(), Email = input.Email.Trim() };
        var result = await userManager.CreateAsync(user, input.Password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, AppRoles.Consulta);
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
