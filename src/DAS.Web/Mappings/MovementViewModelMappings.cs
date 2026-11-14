using DAS.Application.DTOs;
using DAS.Web.Models.ViewModels;

namespace DAS.Web.Mappings;

public static class MovementViewModelMappings
{
    public static RegisterMovementDto ToDto(this MovementInputViewModel input) =>
        new(input.ProductId, input.Type, input.Quantity, input.Reason.Trim(), input.Date);

    public static MovementQueryDto ToQuery(this MovementFilterViewModel filter) =>
        new(filter.ProductId, filter.Type, filter.From, filter.To);
}
