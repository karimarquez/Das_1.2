using DAS.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DAS.Web.Models.ViewModels;

public class MovementListViewModel
{
    public MovementFilterViewModel Filter { get; init; } = new();
    public IReadOnlyList<MovementDto> Movements { get; init; } = [];
    public IEnumerable<SelectListItem> ProductOptions { get; init; } = [];

    public int Count => Movements.Count;
}
