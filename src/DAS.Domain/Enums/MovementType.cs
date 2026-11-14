using System.ComponentModel.DataAnnotations;

namespace DAS.Domain.Enums;

public enum MovementType
{
    [Display(Name = "Entrada")]
    Entrada = 1,

    [Display(Name = "Consumo")]
    Consumo = 2,

    [Display(Name = "Merma")]
    Merma = 3,

    [Display(Name = "Ajuste por conteo")]
    Ajuste = 4
}
