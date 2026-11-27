using System.ComponentModel.DataAnnotations;

namespace DAS.Web.Models.Auth;

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Escribe tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "Escribe un correo electrónico válido.")]
    [StringLength(256, ErrorMessage = "El correo admite como máximo {1} caracteres.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe una contraseña.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre {2} y {1} caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña (mínimo 8 caracteres)")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma la contraseña.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [Display(Name = "Confirmar contraseña")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
