using System.ComponentModel.DataAnnotations;

namespace DAS.Models;

public class MovementInputViewModel
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    public string Type { get; set; } = "Entrada";

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal Quantity { get; set; }

    [Required, StringLength(120)]
    public string Reason { get; set; } = string.Empty;
}