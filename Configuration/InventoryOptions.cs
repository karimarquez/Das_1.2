using System.ComponentModel.DataAnnotations;

namespace DAS.Configuration;

//Se enlaza con la sección "Inventario" de appsettings.json
public sealed class InventoryOptions
{
    public const string SectionName = "Inventario";

    [Range(1, 50)]
    public int RecentMovementsCount { get; set; } = 5;

    [Range(1, 200)]
    public int ProductHistoryCount { get; set; } = 10;

    [Required, StringLength(60)]
    public string DefaultUserName { get; set; } = "Encargado de bar";
}
