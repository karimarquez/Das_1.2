using DAS.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DAS.Web.Models.ViewModels;

public class MovementListViewModel
{
    public MovementFilterViewModel Filter { get; init; } = new();
    public IReadOnlyList<MovementDto> Movements { get; init; } = [];
    public IEnumerable<SelectListItem> ProductOptions { get; init; } = [];
    public int PageSize { get; init; } = 25;
    public int TotalCount { get; init; }

    public int Count => TotalCount;
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Filter.Page > 1;
    public bool HasNextPage => Filter.Page < TotalPages;
}
