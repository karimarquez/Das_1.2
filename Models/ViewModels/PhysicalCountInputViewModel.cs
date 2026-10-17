using System.ComponentModel.DataAnnotations;

namespace DAS.Models.ViewModels;

public class PhysicalCountInputViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un producto.")]
    public int ProductId { get; set; }

    [Display(Name = "Cantidad contada")]
    [Required(ErrorMessage = "Indica la cantidad contada.")]
    [Range(0, 1000000, ErrorMessage = "La cantidad contada debe estar entre {1} y {2}.")]
    public decimal ActualStock { get; set; }
}
