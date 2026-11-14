using System.ComponentModel.DataAnnotations;

namespace DAS.Models.Auth;

public sealed class TokenRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
