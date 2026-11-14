using DAS.Application.Abstractions;
using DAS.Application.Abstractions.Persistence;
using DAS.Application.Common;
using DAS.Application.DTOs;
using DAS.Application.Mappings;
using DAS.Application.Services.Interfaces;
using DAS.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DAS.Application.Services;

public sealed class MovementService(
    IProductRepository products,
    IMovementRepository movements,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<MovementService> logger) : IMovementService
{
    public async Task<IReadOnlyList<MovementDto>> SearchAsync(MovementQueryDto query, CancellationToken cancellationToken = default) =>
        (await movements.SearchAsync(query, cancellationToken)).Select(movement => movement.ToDto()).ToList();

    public async Task<ServiceResult<MovementDto>> RegisterAsync(RegisterMovementDto input, CancellationToken cancellationToken = default)
    {
        if (input.Type is not (MovementType.Entrada or MovementType.Consumo or MovementType.Merma))
        {
            return ServiceResult<MovementDto>.Fail("Los ajustes solo se generan desde el conteo físico.", nameof(RegisterMovementDto.Type));
        }

        if (input.Quantity <= 0)
        {
            return ServiceResult<MovementDto>.Fail("La cantidad debe ser mayor que cero.", nameof(RegisterMovementDto.Quantity));
        }

        if (string.IsNullOrWhiteSpace(input.Reason))
        {
            return ServiceResult<MovementDto>.Fail("Indica el motivo del movimiento.", nameof(RegisterMovementDto.Reason));
        }

        //Una fecha pasada conserva la hora actual; una fecha futura se ignora
        var now = clock.GetLocalNow().DateTime;
        var recordedAt = input.Date is DateTime date && date.Date < now.Date ? date.Date.Add(now.TimeOfDay) : now;

        //La lectura de la existencia y la escritura del movimiento ocurren en la misma transacción
        var result = await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var product = await products.GetByIdAsync(input.ProductId, token);
            if (product is null || !product.IsActive)
            {
                return ServiceResult<MovementDto>.Fail("Selecciona un producto activo.", nameof(RegisterMovementDto.ProductId));
            }

            if (input.Type != MovementType.Entrada && !product.HasStockFor(input.Quantity))
            {
                return ServiceResult<MovementDto>.Fail(
                    $"La existencia disponible de {product.Name} es {product.Stock:0.##} {product.Unit}.",
                    nameof(RegisterMovementDto.Quantity));
            }

            var movement = product.RegisterMovement(input.Type, input.Quantity, input.Reason, currentUser.UserName, recordedAt);
            movements.Add(movement);
            await unitOfWork.SaveChangesAsync(token);
            return ServiceResult<MovementDto>.Ok(movement.ToDto());
        }, cancellationToken);

        if (result.Succeeded)
        {
            var movement = result.Value!;
            logger.LogInformation("{Type} de {Quantity} {Unit} registrado para {ProductName}", movement.Type, movement.Quantity, movement.Unit, movement.ProductName);
        }

        return result;
    }
}
