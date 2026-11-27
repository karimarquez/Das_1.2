using System.ComponentModel.DataAnnotations;

namespace DAS.Web.Models.Auth;

public sealed class TokenRequest
{
    [Required(ErrorMessage = "Escribe el correo electrónico.")]
    [EmailAddress(ErrorMessage = "Escribe un correo electrónico válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe la contraseña.")]
    public string Password { get; set; } = string.Empty;
}
