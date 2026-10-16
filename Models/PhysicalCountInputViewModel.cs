using System.ComponentModel.DataAnnotations;

namespace DAS.Models;

public class PhysicalCountInputViewModel
{
    [Required]
    public int ProductId { get; set; }

    [Range(typeof(decimal), "0", "1000000")]
    public decimal ActualStock { get; set; }
}